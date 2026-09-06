using kssm.be.applications.LoKy.Dtos;
using kssm.be.applications.LoKy.Interfaces;
using kssm.be.external.Mongo.Interfaces;
using kssm.be.external.Pdf.Dtos;
using kssm.be.external.Pdf.Interfaces;
using kssm.be.external.S3.Dtos;
using kssm.be.external.S3.Interfaces;
using kssm.be.external.SaoMai.Interfaces;
using kssm.be.external.Signing.Dtos;
using kssm.be.external.Signing.Interfaces;
using kssm.be.external.Tsa.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants;
using kssm.be.shared.Constants.LoKy;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LoKyFileEntity = kssm.be.domain.LoKy.LoKyFile;
using TemplateEntity = kssm.be.domain.Template.Template;

namespace kssm.be.applications.LoKy.Implements
{
    public class KySoRunner : IKySoRunner
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPdfPreparer _pdfPreparer;
        private readonly IPdfContentWriter _pdfContentWriter;
        private readonly IPdfSignatureInspector _pdfSignatureInspector;
        private readonly ICmsAssembler _cmsAssembler;
        private readonly ISignatureVerifier _signatureVerifier;
        private readonly ISigningKey _signingKey;
        private readonly ITimestampClient _timestampClient;
        private readonly ILoKyFileStorage _loKyFileStorage;
        private readonly IS3FileStorage _s3FileStorage;
        private readonly IS3ClientFactory _s3ClientFactory;
        private readonly IProjectReader _projectReader;
        private readonly IProjectNotifier _projectNotifier;
        private readonly ILogger<KySoRunner> _logger;

        private readonly ConcurrentDictionary<int, CancellationTokenSource> _dangChay = new();

        /// <summary>Khoá nhận việc: hai luồng không được nhận trúng cùng một file.</summary>
        private readonly SemaphoreSlim _khoaNhanViec = new(1, 1);

        public KySoRunner(
            IServiceScopeFactory scopeFactory,
            IPdfPreparer pdfPreparer,
            IPdfContentWriter pdfContentWriter,
            IPdfSignatureInspector pdfSignatureInspector,
            ICmsAssembler cmsAssembler,
            ISignatureVerifier signatureVerifier,
            ISigningKey signingKey,
            ITimestampClient timestampClient,
            ILoKyFileStorage loKyFileStorage,
            IS3FileStorage s3FileStorage,
            IS3ClientFactory s3ClientFactory,
            IProjectReader projectReader,
            IProjectNotifier projectNotifier,
            ILogger<KySoRunner> logger)
        {
            _scopeFactory = scopeFactory;
            _pdfPreparer = pdfPreparer;
            _pdfContentWriter = pdfContentWriter;
            _pdfSignatureInspector = pdfSignatureInspector;
            _cmsAssembler = cmsAssembler;
            _signatureVerifier = signatureVerifier;
            _signingKey = signingKey;
            _timestampClient = timestampClient;
            _loKyFileStorage = loKyFileStorage;
            _s3FileStorage = s3FileStorage;
            _s3ClientFactory = s3ClientFactory;
            _projectReader = projectReader;
            _projectNotifier = projectNotifier;
            _logger = logger;
        }

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

        public void Dung(int loKyId)
        {
            if (_dangChay.TryGetValue(loKyId, out var nguon))
            {
                nguon.Cancel();
            }
        }

        public bool DangChay(int loKyId) => _dangChay.ContainsKey(loKyId);

        public async Task ChayLoAsync(int loKyId, string thumbprint, CancellationToken cancellationToken)
        {
            var phien = await MoPhienAsync(loKyId, thumbprint, cancellationToken);

            var luong = Enumerable.Range(0, LoKyConstants.ParallelFiles)
                .Select(_ => ChayMotLuongAsync(phien, cancellationToken))
                .ToArray();

            // Dừng lô thì các luồng ném OperationCanceledException. Đó là kết thúc BÌNH THƯỜNG, phải nuốt
            // lại — để nó thoát ra ngoài là lô bị ghi thành Lỗi thay vì Tạm dừng hay Huỷ.
            try
            {
                await Task.WhenAll(luong);
            }
            catch (OperationCanceledException)
            {
            }

            // Bị dừng giữa chừng thì trạng thái đã do tầng nghiệp vụ chốt (tạm dừng hoặc huỷ), đụng vào nữa
            // là ghi đè đúng thứ người dùng vừa chọn.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await KetThucLoAsync(loKyId, CancellationToken.None);
        }

        public async Task<PhienKyDto> MoPhienAsync(int loKyId, string thumbprint,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();

            var lo = await db.LoKy.AsNoTracking().FirstAsync(x => x.Id == loKyId, cancellationToken);
            var template = await db.Template.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == lo.TemplateId && !x.Deleted, cancellationToken)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    "Template chữ ký của lô đã bị xoá.");

            // Quan hệ template - toạ độ là quan hệ MỀM nên phải tự nạp theo TemplateId, không có navigation.
            var viTri = await db.TemplatePosition.AsNoTracking()
                .Where(x => x.TemplateId == template.Id && !x.Deleted)
                .Select(x => new PdfPlacementDto
                {
                    Kind = x.Kind,
                    PageNumber = x.PageNumber,
                    XRatio = x.XRatio,
                    YRatio = x.YRatio,
                    WidthRatio = x.WidthRatio,
                    HeightRatio = x.HeightRatio,
                })
                .ToListAsync(cancellationToken);

            var anhChuKyTuoi = await TaiAnhChuKyTuoiAsync(template, cancellationToken);

            // Lấy chứng thư MỘT lần cho cả lô: với token thật, đây chính là chỗ giữ phiên khoá để N file chỉ
            // phải mở khoá một lần — giữ handle khoá, không phải nhớ mã PIN.
            var cert = await _signingKey.LayChungThuAsync(loKyId, thumbprint, cancellationToken);
            var tenNguoiKy = cert.GetNameInfo(X509NameType.SimpleName, false);

            return new PhienKyDto
            {
                LoKyId = loKyId,
                ProjectId = lo.ProjectId,
                Cert = cert,
                ChuoiChungThu = _signingKey.LayChuoiChungThu(cert),
                TuyChonMau = DungTuyChon(template, viTri, tenNguoiKy, anhChuKyTuoi),
                KyDe = template.KyDe,
                Kho = await LayKhoAsync(lo.ProjectId, cancellationToken),
                TienToDaKy = lo.TienToKho ?? LoKyConstants.GetSignedPrefix(loKyId),
            };
        }

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

        public async Task<int?> NhanViecAsync(int loKyId, CancellationToken cancellationToken)
        {
            await _khoaNhanViec.WaitAsync(cancellationToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();

                // Lấy việc kế tiếp LUÔN lọc theo trạng thái Cho, nên ký tiếp sau khi tạm dừng không bao giờ
                // ký đè lên file đã Xong.
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

        public async Task KyMotFileAsync(int loKyFileId, PhienKyDto phien,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();

            var file = await db.LoKyFile.FirstOrDefaultAsync(x => x.Id == loKyFileId, cancellationToken);
            if (file == null)
            {
                return;
            }

            var thanhCong = false;
            var dongHo = System.Diagnostics.Stopwatch.StartNew();
            long msTai = 0, msDung = 0, msKy = 0, msTsa = 0;

            try
            {
                var pdf = await _loKyFileStorage.DownloadAsync(phien.Kho, file.ObjectKeyNguon, cancellationToken);
                msTai = dongHo.ElapsedMilliseconds;

                // Chốt cửa trước khi dựng bản ký: file đã có chữ ký chỉ được ký thêm khi template bật cờ ký
                // đè. Đánh trượt RIÊNG file này để người dùng thấy đúng file nào bị chặn, cả lô vẫn chạy tiếp.
                if (!phien.KyDe && _pdfSignatureInspector.HasSignature(pdf))
                {
                    throw new UserFriendlyException(ErrorCodes.PdfAlreadySigned,
                        "File đã có chữ ký số. Bật \"Cho phép ký đè\" ở template nếu vẫn muốn ký thêm.");
                }

                var signedAt = DateTimeConstants.VietnamNow;
                var prepared = _pdfPreparer.Prepare(pdf, NhanBanTuyChon(phien.TuyChonMau, signedAt));

                // Đúng bộ byte mà về sau máy người dùng sẽ ký bằng token: máy chủ băm nội dung, dựng thuộc
                // tính, còn phép ký thì nằm ở nơi giữ khoá.
                var hash = SHA256.HashData(prepared.NoiDungKy);
                var signedAttributes = _cmsAssembler.BuildSignedAttributes(hash, phien.Cert, DateTime.UtcNow);
                msDung = dongHo.ElapsedMilliseconds - msTai;

                // KHÔNG khoá tuần tự ở đây: token vẫn ký lần lượt, nhưng việc xếp hàng do chính nơi giữ khoá
                // lo. Khoá ở đây thì mỗi lúc chỉ có một yêu cầu bay sang máy người dùng, và việc gom tám yêu
                // cầu thành một đợt trở nên vô nghĩa.
                var chuKyTho = await _signingKey.KyAsync(file.LoKyId, signedAttributes, phien.Cert,
                    cancellationToken);
                msKy = dongHo.ElapsedMilliseconds - msTai - msDung;

                var tsaToken = await _timestampClient.RequestTokenAsync(chuKyTho, cancellationToken);
                msTsa = dongHo.ElapsedMilliseconds - msTai - msDung - msKy;
                var cms = _cmsAssembler.Assemble(signedAttributes, chuKyTho, phien.Cert,
                    phien.ChuoiChungThu, tsaToken);

                var daKy = _pdfContentWriter.Write(prepared, cms);
                KiemChuKy(file, daKy);

                file.ObjectKeyDaKy = phien.TienToDaKy + file.TenFile;
                await _loKyFileStorage.UploadAsync(phien.Kho, daKy, file.ObjectKeyDaKy,
                    LoKyConstants.PdfContentType, cancellationToken);

                var genTime = _timestampClient.DocGenTime(tsaToken);
                file.TrangThai = TrangThaiFileKy.Xong;
                file.ThoiGianKy = signedAt;
                file.DauThoiGian = genTime.HasValue ? DateTimeConstants.ToVietnamTime(genTime.Value) : null;
                file.LyDoLoi = null;
                thanhCong = true;
            }
            catch (OperationCanceledException)
            {
                // Lô bị dừng giữa chừng: trả file về hàng đợi để lần chạy sau ký lại từ đúng chỗ này, và
                // KHÔNG đếm nó vào số lỗi — người dùng chỉ tạm dừng chứ file có hỏng đâu.
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

            // Chia nhỏ thời gian từng chặng: không có bảng này thì mọi phán đoán về chỗ chậm đều là đoán mò.
            _logger.LogInformation(
                "Ky file {ThuTu}: tong {Tong}ms (tai {Tai}ms, dung {Dung}ms, cho chu ky {Ky}ms, tsa {Tsa}ms, day len kho {Day}ms)",
                file.ThuTu, dongHo.ElapsedMilliseconds, msTai, msDung, msKy, msTsa,
                dongHo.ElapsedMilliseconds - msTai - msDung - msKy - msTsa);
        }

        /// <summary>
        /// Đọc lại chữ ký trên bản vừa dựng rồi ghi kết quả vào chính dòng file. Không hợp lệ thì ném để
        /// đánh trượt RIÊNG file đó: đẩy một bản ký hỏng lên kho rồi giao cho người dùng còn tệ hơn hẳn.
        /// </summary>
        public void KiemChuKy(LoKyFileEntity file, byte[] daKy)
        {
            var chuKy = _pdfSignatureInspector.ReadSignature(daKy);
            var ketQua = chuKy == null
                ? new KiemChuKyDto { LyDo = "Không tìm thấy khối chữ ký trong bản vừa ký." }
                : _signatureVerifier.Verify(chuKy);

            file.ChuKyHopLe = ketQua.HopLe;
            file.LyDoChuKy = ketQua.LyDo;

            if (!ketQua.HopLe)
            {
                throw new UserFriendlyException(ErrorCodes.SignatureAssembleFailed, ketQua.LyDo!);
            }
        }

        public async Task<byte[]?> TaiAnhChuKyTuoiAsync(TemplateEntity template,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(template.AnhChuKyTuoiObjectKey))
            {
                return null;
            }

            try
            {
                // Ảnh của template luôn nằm ở kho mặc định của service, không phải kho của dự án.
                return await _s3FileStorage.DownloadAsync(template.AnhChuKyTuoiObjectKey, cancellationToken);
            }
            catch (Exception ex)
            {
                // DỪNG cả lô chứ không lặng lẽ ký thiếu ảnh: template đã khai có chữ ký tươi thì người dùng
                // đang chờ nó xuất hiện trên giấy, mà phát hiện ra cả nghìn tờ thiếu chữ ký sau khi đã ký
                // xong thì phải ký lại toàn bộ. Nêu thẳng object key để biết đường tải ảnh lên lại.
                _logger.LogError(ex, "Không tải được ảnh chữ ký tươi {Key} của template {TemplateId}",
                    template.AnhChuKyTuoiObjectKey, template.Id);

                throw new UserFriendlyException(ErrorCodes.TemplateImageInvalid,
                    $"Không tải được ảnh chữ ký tươi của template \"{template.TenTemplate}\" từ kho "
                    + $"(object key: {template.AnhChuKyTuoiObjectKey}). Vào màn Template tải lại ảnh rồi ký lại.");
            }
        }

        public PdfPrepareOptionsDto DungTuyChon(TemplateEntity template, List<PdfPlacementDto> viTri,
            string tenNguoiKy, byte[]? anhChuKyTuoi)
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
                MauChuKySo = template.MauChuKySo,
                MauChuKyTuoi = template.MauChuKyTuoi,
                ViTri = viTri,
            };
        }

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
                MauChuKySo = mau.MauChuKySo,
                MauChuKyTuoi = mau.MauChuKyTuoi,
                ViTri = mau.ViTri,
            };
        }

        public async Task CongDonKetQuaAsync(int loKyId, bool thanhCong)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();
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

        public async Task KetThucLoAsync(int loKyId, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();

            var lo = await db.LoKy.FirstOrDefaultAsync(x => x.Id == loKyId, cancellationToken);
            if (lo == null)
            {
                return;
            }

            lo.TrangThai = TrangThaiLoKy.Xong;
            lo.ThoiDiemXong = DateTimeConstants.VietnamNow;
            lo.ModifiedDate = lo.ThoiDiemXong;
            await db.SaveChangesAsync(cancellationToken);

            await DonNguonAsync(lo.Id, lo.ProjectId, cancellationToken);

            if (string.IsNullOrWhiteSpace(lo.ProjectId) || lo.DaBaoDuAnKyXong)
            {
                return;
            }

            // Báo sang sao_mai để dự án chuyển trạng thái. Bên đó không nhận được thì chỉ ghi cờ là chưa báo:
            // file đã ký vẫn nằm nguyên trên kho, không có lý do gì để coi cả lô là hỏng.
            if (await _projectNotifier.NotifySignedAsync(lo.ProjectId, lo.Id, lo.DaXong, lo.SoLoi,
                cancellationToken))
            {
                lo.DaBaoDuAnKyXong = true;
                lo.ModifiedDate = DateTimeConstants.VietnamNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task GhiLoiChungAsync(int loKyId, string thongDiep)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();

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

        /// <summary>
        /// Kho của lô: MinIO riêng của dự án; kho mặc định cho lô nhận file tải lên và cho dự án chưa khai
        /// đủ khoá MinIO.
        /// </summary>
        public async Task<S3ConnectionDto> LayKhoAsync(string? projectId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                return _s3ClientFactory.DefaultConnection();
            }

            return await _projectReader.GetProjectStorageAsync(projectId, cancellationToken)
                ?? _s3ClientFactory.DefaultConnection();
        }

        /// <summary>
        /// Dọn bản nguồn của lô nhận file tải lên. Lô của dự án KHÔNG dọn gì: bản nguồn ở đó là tài liệu thật
        /// của dự án, luồng ký chỉ đọc chứ không sở hữu chúng.
        /// </summary>
        public async Task DonNguonAsync(int loKyId, string? projectId, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                return;
            }

            try
            {
                await _loKyFileStorage.DeleteByPrefixAsync(_s3ClientFactory.DefaultConnection(),
                    LoKyConstants.GetSourcePrefix(loKyId), cancellationToken);
            }
            catch (Exception ex)
            {
                // Dọn hỏng không được phép làm lô đã ký xong bị coi là lỗi: file đã ký vẫn nguyên vẹn.
                _logger.LogWarning(ex, "Dọn file nguồn của lô {LoKyId} thất bại", loKyId);
            }
        }
    }
}
