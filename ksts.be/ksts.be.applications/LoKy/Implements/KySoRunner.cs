using ksts.be.applications.LoKy.Dtos;
using ksts.be.applications.LoKy.Interfaces;
using ksts.be.external.Pdf.Dtos;
using ksts.be.external.Pdf.Interfaces;
using ksts.be.external.Signing.Interfaces;
using ksts.be.external.Storage.Interfaces;
using ksts.be.external.Tsa.Interfaces;
using ksts.be.infrastructure.Persistence;
using ksts.be.shared.Constants;
using ksts.be.shared.Constants.LoKy;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using TemplateEntity = ksts.be.domain.Template.Template;

namespace ksts.be.applications.LoKy.Implements
{
    /// <inheritdoc/>
    public class KySoRunner : IKySoRunner
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPdfPreparer _pdfPreparer;
        private readonly IPdfContentWriter _pdfContentWriter;
        private readonly ICmsAssembler _cmsAssembler;
        private readonly ISigningKey _signingKey;
        private readonly ITimestampClient _timestampClient;
        private readonly ILoKyFileStorage _loKyFileStorage;
        private readonly IS3FileStorage _s3FileStorage;
        private readonly ILogger<KySoRunner> _logger;

        private readonly ConcurrentDictionary<int, CancellationTokenSource> _dangChay = new();

        /// <summary>Khoá nhận việc: hai luồng không được nhận trúng cùng một file.</summary>
        private readonly SemaphoreSlim _khoaNhanViec = new(1, 1);

        public KySoRunner(
            IServiceScopeFactory scopeFactory,
            IPdfPreparer pdfPreparer,
            IPdfContentWriter pdfContentWriter,
            ICmsAssembler cmsAssembler,
            ISigningKey signingKey,
            ITimestampClient timestampClient,
            ILoKyFileStorage loKyFileStorage,
            IS3FileStorage s3FileStorage,
            ILogger<KySoRunner> logger)
        {
            _scopeFactory = scopeFactory;
            _pdfPreparer = pdfPreparer;
            _pdfContentWriter = pdfContentWriter;
            _cmsAssembler = cmsAssembler;
            _signingKey = signingKey;
            _timestampClient = timestampClient;
            _loKyFileStorage = loKyFileStorage;
            _s3FileStorage = s3FileStorage;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void BatDau(int loKyId, string thumbprint)
        {
            _logger.LogInformation("{Method} loKyId={LoKyId}", nameof(BatDau), loKyId);

            var nguon = new CancellationTokenSource();
            if (!_dangChay.TryAdd(loKyId, nguon))
            {
                nguon.Dispose();
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await ChayLoAsync(loKyId, thumbprint, nguon.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lô ký {LoKyId} dừng vì sự cố chung", loKyId);
                    await GhiLoiChungAsync(loKyId, ex.Message);
                }
                finally
                {
                    if (_dangChay.TryRemove(loKyId, out var xong)) xong.Dispose();
                }
            });
        }

        /// <inheritdoc/>
        public void Dung(int loKyId)
        {
            if (_dangChay.TryGetValue(loKyId, out var nguon))
            {
                nguon.Cancel();
            }
        }

        /// <inheritdoc/>
        public bool DangChay(int loKyId) => _dangChay.ContainsKey(loKyId);

        /// <inheritdoc/>
        public async Task ChayLoAsync(int loKyId, string thumbprint, CancellationToken cancellationToken)
        {
            var phien = await MoPhienAsync(loKyId, thumbprint, cancellationToken);

            var luong = Enumerable.Range(0, LoKyConstants.SoFileSongSong)
                .Select(_ => ChayMotLuongAsync(phien, cancellationToken))
                .ToArray();

            // Người dùng bấm Huỷ thì các luồng ném OperationCanceledException. Đó là kết thúc BÌNH THƯỜNG,
            // phải nuốt lại để còn chốt trạng thái Huỷ — để nó thoát ra ngoài là lô bị ghi thành Lỗi.
            try
            {
                await Task.WhenAll(luong);
            }
            catch (OperationCanceledException)
            {
            }

            await KetThucLoAsync(loKyId, cancellationToken.IsCancellationRequested);
        }

        /// <inheritdoc/>
        public async Task<PhienKyDto> MoPhienAsync(int loKyId, string thumbprint,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KstsDbContext>();

            var lo = await db.LoKy.AsNoTracking().FirstAsync(x => x.Id == loKyId, cancellationToken);
            var template = await db.Template
                .Include(x => x.Positions.Where(p => !p.Deleted))
                .AsNoTracking()
                .FirstAsync(x => x.Id == lo.TemplateId, cancellationToken);

            var anhChuKyTuoi = await TaiAnhChuKyTuoiAsync(template, cancellationToken);

            // Lấy chứng thư MỘT lần cho cả lô. Với token thật, đây chính là chỗ giữ phiên khoá để N file chỉ
            // phải mở khoá một lần — giữ handle khoá, không phải nhớ mã PIN.
            var cert = await _signingKey.LayChungThuAsync(loKyId, thumbprint, cancellationToken);
            var tenNguoiKy = cert.GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType
                .SimpleName, false);

            return new PhienKyDto
            {
                LoKyId = loKyId,
                Cert = cert,
                ChuoiChungThu = _signingKey.LayChuoiChungThu(cert),
                TuyChonMau = DungTuyChon(template, tenNguoiKy, anhChuKyTuoi),
            };
        }

        /// <inheritdoc/>
        public async Task ChayMotLuongAsync(PhienKyDto phien, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var fileId = await NhanViecAsync(phien.LoKyId, cancellationToken);
                if (fileId == null)
                {
                    break;
                }

                await KyMotFileAsync(fileId.Value, phien, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<int?> NhanViecAsync(int loKyId, CancellationToken cancellationToken)
        {
            await _khoaNhanViec.WaitAsync(cancellationToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<KstsDbContext>();

                // Lấy việc kế tiếp LUÔN lọc theo trạng thái Cho, nên chạy tiếp sau khi dừng giữa chừng không
                // bao giờ ký đè lên file đã Xong.
                var ke = await db.LoKyFile
                    .Where(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.Cho)
                    .OrderBy(x => x.ThuTu)
                    .FirstOrDefaultAsync(cancellationToken);

                if (ke == null)
                {
                    return null;
                }

                ke.TrangThai = TrangThaiFileKy.DangKy;
                ke.ModifiedDate = DateTimeConstants.VietnamNow;
                await db.SaveChangesAsync(cancellationToken);

                return ke.Id;
            }
            finally
            {
                _khoaNhanViec.Release();
            }
        }

        /// <inheritdoc/>
        public async Task KyMotFileAsync(int loKyFileId, PhienKyDto phien,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KstsDbContext>();

            var file = await db.LoKyFile.FirstOrDefaultAsync(x => x.Id == loKyFileId, cancellationToken);
            if (file == null)
            {
                return;
            }

            var thanhCong = false;
            try
            {
                var pdf = await _loKyFileStorage.TaiAsync(file.ObjectKeyNguon, cancellationToken);

                var signedAt = DateTimeConstants.VietnamNow;
                var prepared = _pdfPreparer.Prepare(pdf, NhanBanTuyChon(phien.TuyChonMau, signedAt));

                // Đúng bộ byte mà về sau plugin sẽ nhận và ký bằng token: server băm nội dung, dựng thuộc
                // tính, còn phép ký thì nằm ở nơi giữ khoá.
                var hash = SHA256.HashData(prepared.NoiDungKy);
                var signedAttributes = _cmsAssembler.BuildSignedAttributes(hash, phien.Cert, DateTime.UtcNow);

                // KHÔNG khoá tuần tự ở đây: token vẫn ký lần lượt, nhưng việc xếp hàng do chính nơi giữ khoá
                // lo. Khoá ở đây thì mỗi lúc chỉ có đúng một yêu cầu bay sang máy người dùng, và mỗi file
                // phải chịu trọn một vòng đi-về; thả ra thì tám luồng cùng gửi, hàng đợi gom cả tám vào một
                // đợt và chi phí đường truyền chia đều cho ngần ấy file.
                var chuKyTho = await _signingKey.KyAsync(file.LoKyId, signedAttributes, phien.Cert,
                    cancellationToken);

                var tsaToken = await _timestampClient.RequestTokenAsync(chuKyTho, cancellationToken);
                var cms = _cmsAssembler.Assemble(signedAttributes, chuKyTho, phien.Cert,
                    phien.ChuoiChungThu, tsaToken);

                var daKy = _pdfContentWriter.Write(prepared, cms);
                file.ObjectKeyDaKy = await _loKyFileStorage.LuuFileDaKyAsync(file.LoKyId, file.ThuTu, daKy,
                    cancellationToken);

                await _s3FileStorage.CopyAsync(file.ObjectKeyDaKy, LoKyConstants.GetKhoDaKyKey(file.TenFile),
                    cancellationToken);

                var genTime = _timestampClient.DocGenTime(tsaToken);
                file.TrangThai = TrangThaiFileKy.Xong;
                file.ThoiGianKy = signedAt;
                file.DauThoiGian = genTime.HasValue ? DateTimeConstants.ToVietnamTime(genTime.Value) : null;
                file.LyDoLoi = null;
                thanhCong = true;
            }
            catch (OperationCanceledException)
            {
                // Lô bị dừng giữa chừng: trả file về hàng đợi để lần chạy sau ký lại từ đúng chỗ này.
                file.TrangThai = TrangThaiFileKy.Cho;
                file.ModifiedDate = DateTimeConstants.VietnamNow;
                await db.SaveChangesAsync(CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ký file {FileId} thất bại", loKyFileId);
                file.TrangThai = TrangThaiFileKy.Loi;
                file.LyDoLoi = ex.Message;
            }

            file.ModifiedDate = DateTimeConstants.VietnamNow;
            await db.SaveChangesAsync(CancellationToken.None);
            await CongDonKetQuaAsync(file.LoKyId, thanhCong);
        }

        /// <inheritdoc/>
        public async Task<byte[]?> TaiAnhChuKyTuoiAsync(TemplateEntity template,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(template.AnhChuKyTuoiObjectKey))
            {
                return null;
            }

            try
            {
                return await _s3FileStorage.DownloadAsync(template.AnhChuKyTuoiObjectKey, cancellationToken);
            }
            catch (Exception ex)
            {
                // DỪNG cả lô chứ không lặng lẽ ký thiếu ảnh: template đã khai có chữ ký tươi thì người dùng
                // đang chờ nó xuất hiện trên giấy, mà phát hiện ra 5000 tờ thiếu chữ ký sau khi đã ký xong
                // thì phải ký lại toàn bộ. Nêu thẳng object key để biết đường tải ảnh lên lại.
                _logger.LogError(ex, "Không tải được ảnh chữ ký tươi {Key} của template {TemplateId}",
                    template.AnhChuKyTuoiObjectKey, template.Id);

                throw new UserFriendlyException(ErrorCodes.TemplateImageInvalid,
                    $"Không tải được ảnh chữ ký tươi của template \"{template.TenTemplate}\" từ kho "
                    + $"(object key: {template.AnhChuKyTuoiObjectKey}). Vào màn Template tải lại ảnh rồi ký lại.");
            }
        }

        /// <inheritdoc/>
        public PdfPrepareOptionsDto DungTuyChon(TemplateEntity template, string tenNguoiKy,
            byte[]? anhChuKyTuoi)
        {
            return new PdfPrepareOptionsDto
            {
                TenNguoiKy = string.IsNullOrWhiteSpace(tenNguoiKy)
                    ? template.TenChungThu ?? string.Empty
                    : tenNguoiKy,
                LyDoKy = template.LyDoKy,
                NoiKy = template.NoiKy,
                HienThiChuKySo = template.HienThiChuKySo,
                NhoiChuKySoVaoAnh = template.NhoiChuKySoVaoAnh,
                AnhChuKyTuoi = anhChuKyTuoi,
                DoDamChuKyTuoi = template.DoDamChuKyTuoi,
                DoDayNetChuKyTuoi = template.DoDayNetChuKyTuoi,
                ViTri = template.Positions.Select(p => new PdfPlacementDto
                {
                    Kind = p.Kind,
                    PageNumber = p.PageNumber,
                    XRatio = p.XRatio,
                    YRatio = p.YRatio,
                    WidthRatio = p.WidthRatio,
                    HeightRatio = p.HeightRatio,
                }).ToList(),
            };
        }

        /// <inheritdoc/>
        public PdfPrepareOptionsDto NhanBanTuyChon(PdfPrepareOptionsDto mau, DateTime signedAt)
        {
            return new PdfPrepareOptionsDto
            {
                TenNguoiKy = mau.TenNguoiKy,
                LyDoKy = mau.LyDoKy,
                NoiKy = mau.NoiKy,
                SignedAt = signedAt,
                HienThiChuKySo = mau.HienThiChuKySo,
                NhoiChuKySoVaoAnh = mau.NhoiChuKySoVaoAnh,
                AnhChuKyTuoi = mau.AnhChuKyTuoi,
                DoDamChuKyTuoi = mau.DoDamChuKyTuoi,
                DoDayNetChuKyTuoi = mau.DoDayNetChuKyTuoi,
                ViTri = mau.ViTri,
            };
        }

        /// <inheritdoc/>
        public async Task CongDonKetQuaAsync(int loKyId, bool thanhCong)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KstsDbContext>();
            var now = DateTimeConstants.VietnamNow;

            // Cộng dồn bằng MỘT câu lệnh thay vì đếm lại cả bảng sau mỗi file: đếm lại là hai lần quét bảng
            // cho mỗi file, mà nhiều luồng cùng đếm còn ghi đè kết quả của nhau.
            if (thanhCong)
            {
                await db.LoKy.Where(x => x.Id == loKyId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.DaXong, x => x.DaXong + 1)
                        .SetProperty(x => x.ModifiedDate, now));
                return;
            }

            await db.LoKy.Where(x => x.Id == loKyId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.SoLoi, x => x.SoLoi + 1)
                    .SetProperty(x => x.ModifiedDate, now));
        }

        /// <inheritdoc/>
        public async Task KetThucLoAsync(int loKyId, bool biHuy)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KstsDbContext>();

            var lo = await db.LoKy.FirstOrDefaultAsync(x => x.Id == loKyId);
            if (lo == null)
            {
                return;
            }

            lo.TrangThai = biHuy ? TrangThaiLoKy.Huy : TrangThaiLoKy.Xong;
            lo.ThoiDiemXong = DateTimeConstants.VietnamNow;
            lo.ModifiedDate = lo.ThoiDiemXong;

            await db.SaveChangesAsync();

            if (biHuy)
            {
                return;
            }

            // Ký xong thì bản nguồn hết việc — dọn ngay để lô không nằm lại trên kho. CHỈ dọn khi lô chạy
            // trọn: lô bị huỷ còn phải ký tiếp từ file dở, xoá nguồn là mất luôn đường chạy tiếp.
            try
            {
                await _loKyFileStorage.XoaThuMucAsync(loKyId, LoKyConstants.ThuMucNguon);
            }
            catch (Exception ex)
            {
                // Dọn hỏng không được phép làm lô đã ký xong bị coi là lỗi: file đã ký vẫn nguyên vẹn.
                _logger.LogWarning(ex, "Dọn file nguồn của lô {LoKyId} thất bại", loKyId);
            }
        }

        /// <inheritdoc/>
        public async Task GhiLoiChungAsync(int loKyId, string thongDiep)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KstsDbContext>();

            var lo = await db.LoKy.FirstOrDefaultAsync(x => x.Id == loKyId);
            if (lo == null)
            {
                return;
            }

            lo.TrangThai = TrangThaiLoKy.Loi;
            lo.LoiChung = thongDiep;
            lo.ThoiDiemXong = DateTimeConstants.VietnamNow;
            lo.ModifiedDate = lo.ThoiDiemXong;

            await db.SaveChangesAsync();
        }
    }
}
