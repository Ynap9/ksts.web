using kssm.be.applications.KySo.LoKy.Dtos;
using kssm.be.applications.KySo.LoKy.Interfaces;
using kssm.be.external.Drive.Interfaces;
using kssm.be.external.KySo.Pdf.Dtos;
using kssm.be.external.KySo.Pdf.Interfaces;
using kssm.be.external.KySo.SaoMai.Interfaces;
using kssm.be.external.KySo.Signing.Dtos;
using kssm.be.external.KySo.Signing.Interfaces;
using kssm.be.external.KySo.Tsa.Interfaces;
using kssm.be.external.Mongo.Interfaces;
using kssm.be.external.S3.Dtos;
using kssm.be.external.S3.Interfaces;

using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants;
using kssm.be.shared.Constants.LoKy;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using TemplateEntity = kssm.be.domain.KySo.Template.Template;

namespace kssm.be.applications.KySo.LoKy.Implements
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
        private readonly IDriveFileStorage _driveFileStorage;
        private readonly IS3FileStorage _s3FileStorage;
        private readonly IS3ClientFactory _s3ClientFactory;
        private readonly IProjectReader _projectReader;
        private readonly IProjectNotifier _projectNotifier;
        private readonly ILogger<KySoRunner> _logger;

        private readonly ConcurrentDictionary<int, CancellationTokenSource> _dangChay = new();

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
            IDriveFileStorage driveFileStorage,
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
            _driveFileStorage = driveFileStorage;
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

            using var dungLo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var viec = Channel.CreateBounded<ViecKyDto>(LoKyConstants.ParallelFiles);
            var banKy = Channel.CreateBounded<BanKyDto>(LoKyConstants.ParallelFinishingFiles);

            var luongKy = Enumerable.Range(0, LoKyConstants.ParallelFiles)
                .Select(_ => ChayCoKiemSoatAsync(
                    () => ChayLuongKyAsync(phien, viec.Reader, banKy.Writer, dungLo.Token), dungLo))
                .ToList();

            var tacVu = new List<Task>(luongKy)
            {
                ChayCoKiemSoatAsync(() => NhanViecAsync(loKyId, viec.Writer, dungLo.Token), dungLo),
                DongKenhKhiXongAsync(luongKy, banKy.Writer),
            };

            tacVu.AddRange(Enumerable.Range(0, LoKyConstants.ParallelFinishingFiles)
                .Select(_ => ChayCoKiemSoatAsync(
                    () => ChayLuongHoanTatAsync(phien, banKy.Reader, dungLo.Token), dungLo)));

            try
            {
                await Task.WhenAll(tacVu);
            }
            catch (Exception)
            {
            }

            var suCo = tacVu
                .Where(x => x.IsFaulted)
                .SelectMany(x => x.Exception!.InnerExceptions)
                .FirstOrDefault(x => x is not OperationCanceledException);

            if (dungLo.IsCancellationRequested)
            {
                await TraViecDangKyAsync(loKyId);
            }

            if (suCo != null)
            {
                ExceptionDispatchInfo.Capture(suCo).Throw();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await KetThucLoAsync(loKyId, CancellationToken.None);
        }

        public async Task ChayCoKiemSoatAsync(Func<Task> viec, CancellationTokenSource dungLo)
        {
            try
            {
                await viec();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                dungLo.Cancel();
                throw;
            }
        }

        public async Task DongKenhKhiXongAsync(IEnumerable<Task> luongKy, ChannelWriter<BanKyDto> banKy)
        {
            try
            {
                await Task.WhenAll(luongKy);
            }
            finally
            {
                banKy.TryComplete();
            }
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

            var anhDauDo = await TaiAnhDauDoAsync(template, cancellationToken);
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
                TuyChonMau = DungTuyChon(template, viTri, tenNguoiKy, anhDauDo, anhChuKyTuoi),
                KyDe = template.KyDe,
                Kho = await LayKhoAsync(lo.ProjectId, cancellationToken),
                DriveFolderId = lo.DriveFolderId ?? throw new UserFriendlyException(
                    ErrorCodes.DriveFolderFailed, "Lô ký chưa có thư mục đích trên Google Drive."),
            };
        }

        public async Task NhanViecAsync(int loKyId, ChannelWriter<ViecKyDto> viec,
            CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    var dot = await NhanDotViecAsync(loKyId, cancellationToken);
                    if (dot.Count == 0)
                    {
                        return;
                    }

                    foreach (var item in dot)
                    {
                        await viec.WriteAsync(item, cancellationToken);
                    }
                }
            }
            finally
            {
                viec.TryComplete();
            }
        }

        public async Task<List<ViecKyDto>> NhanDotViecAsync(int loKyId, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();

            var dot = await db.LoKyFile
                .AsNoTracking()
                .Where(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.Cho)
                .OrderBy(x => x.ThuTu)
                .Take(LoKyConstants.ParallelFiles)
                .Select(x => new ViecKyDto
                {
                    Id = x.Id,
                    LoKyId = x.LoKyId,
                    ThuTu = x.ThuTu,
                    TenFile = x.TenFile,
                    ObjectKeyNguon = x.ObjectKeyNguon,
                })
                .ToListAsync(cancellationToken);

            if (dot.Count == 0)
            {
                return dot;
            }

            var ids = dot.Select(x => x.Id).ToList();
            var now = DateTimeConstants.VietnamNow;

            await db.LoKyFile
                .Where(x => ids.Contains(x.Id) && x.TrangThai == TrangThaiFileKy.Cho)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.TrangThai, TrangThaiFileKy.DangKy)
                    .SetProperty(x => x.ModifiedDate, now), cancellationToken);

            return dot;
        }

        public async Task ChayLuongKyAsync(PhienKyDto phien, ChannelReader<ViecKyDto> viec,
            ChannelWriter<BanKyDto> banKy, CancellationToken cancellationToken)
        {
            await foreach (var item in viec.ReadAllAsync(cancellationToken))
            {
                var ban = await DungBanKyAsync(item, phien, cancellationToken);
                if (ban != null)
                {
                    await banKy.WriteAsync(ban, cancellationToken);
                }
            }
        }

        public async Task ChayLuongHoanTatAsync(PhienKyDto phien, ChannelReader<BanKyDto> banKy,
            CancellationToken cancellationToken)
        {
            await foreach (var ban in banKy.ReadAllAsync(cancellationToken))
            {
                await HoanTatBanKyAsync(ban, phien, cancellationToken);
            }
        }

        public async Task<BanKyDto?> DungBanKyAsync(ViecKyDto viec, PhienKyDto phien,
            CancellationToken cancellationToken)
        {
            var dongHo = Stopwatch.StartNew();
            long msTai = 0, msDung = 0;

            try
            {
                var pdf = await _loKyFileStorage.DownloadAsync(phien.Kho, viec.ObjectKeyNguon, cancellationToken);
                msTai = dongHo.ElapsedMilliseconds;

                if (!phien.KyDe && _pdfSignatureInspector.HasSignature(pdf))
                {
                    throw new UserFriendlyException(ErrorCodes.PdfAlreadySigned,
                        "File đã có chữ ký số. Bật \"Cho phép ký đè\" ở template nếu vẫn muốn ký thêm.");
                }

                var signedAt = DateTimeConstants.VietnamNow;
                var prepared = _pdfPreparer.Prepare(pdf, NhanBanTuyChon(phien.TuyChonMau, signedAt));

                var hash = SHA256.HashData(prepared.NoiDungKy);
                var signedAttributes = _cmsAssembler.BuildSignedAttributes(hash, phien.Cert, DateTime.UtcNow);
                msDung = dongHo.ElapsedMilliseconds - msTai;

                var chuKyTho = await _signingKey.KyAsync(viec.LoKyId, signedAttributes, phien.Cert,
                    cancellationToken);

                return new BanKyDto
                {
                    Viec = viec,
                    Prepared = prepared,
                    SignedAttributes = signedAttributes,
                    ChuKyTho = chuKyTho,
                    SignedAt = signedAt,
                    MsTai = msTai,
                    MsDung = msDung,
                    MsKy = dongHo.ElapsedMilliseconds - msTai - msDung,
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Ký file {FileId} thất bại", viec.Id);
                await GhiKetQuaLoiAsync(viec, ex.Message, null);
                GhiNhatKyThoiGian(viec.ThuTu, dongHo.ElapsedMilliseconds, msTai, msDung, 0, 0);
                return null;
            }
        }

        public async Task HoanTatBanKyAsync(BanKyDto ban, PhienKyDto phien, CancellationToken cancellationToken)
        {
            var dongHo = Stopwatch.StartNew();
            long msTsa = 0;
            KiemChuKyDto? kiem = null;
            string driveFileId;
            DateTime? dauThoiGian;

            try
            {
                var tsaToken = await _timestampClient.RequestTokenAsync(ban.ChuKyTho, cancellationToken);
                msTsa = dongHo.ElapsedMilliseconds;

                var cms = _cmsAssembler.Assemble(ban.SignedAttributes, ban.ChuKyTho, phien.Cert,
                    phien.ChuoiChungThu, tsaToken);
                var daKy = _pdfContentWriter.Write(ban.Prepared, cms);

                kiem = KiemChuKy(daKy);
                if (!kiem.HopLe)
                {
                    throw new UserFriendlyException(ErrorCodes.SignatureAssembleFailed, kiem.LyDo!);
                }

                driveFileId = await _driveFileStorage.UploadAsync(phien.DriveFolderId, daKy,
                    ban.Viec.TenFile, LoKyConstants.PdfContentType, cancellationToken);

                var genTime = _timestampClient.DocGenTime(tsaToken);
                dauThoiGian = genTime.HasValue ? DateTimeConstants.ToVietnamTime(genTime.Value) : null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Ký file {FileId} thất bại", ban.Viec.Id);
                await GhiKetQuaLoiAsync(ban.Viec, ex.Message, kiem);
                GhiNhatKyThoiGian(ban.Viec.ThuTu, ban.MsTai + ban.MsDung + ban.MsKy + dongHo.ElapsedMilliseconds,
                    ban.MsTai, ban.MsDung, ban.MsKy, msTsa);
                return;
            }

            await GhiKetQuaXongAsync(ban, driveFileId, dauThoiGian, kiem!);
            GhiNhatKyThoiGian(ban.Viec.ThuTu, ban.MsTai + ban.MsDung + ban.MsKy + dongHo.ElapsedMilliseconds,
                ban.MsTai, ban.MsDung, ban.MsKy, msTsa);
        }

        public KiemChuKyDto KiemChuKy(byte[] daKy)
        {
            var chuKy = _pdfSignatureInspector.ReadSignature(daKy);

            return chuKy == null
                ? new KiemChuKyDto { LyDo = "Không tìm thấy khối chữ ký trong bản vừa ký." }
                : _signatureVerifier.Verify(chuKy);
        }

        public async Task GhiKetQuaXongAsync(BanKyDto ban, string driveFileId, DateTime? dauThoiGian,
            KiemChuKyDto kiem)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();
            var now = DateTimeConstants.VietnamNow;

            await db.LoKyFile
                .Where(x => x.Id == ban.Viec.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.DriveFileId, driveFileId)
                    .SetProperty(x => x.TrangThai, TrangThaiFileKy.Xong)
                    .SetProperty(x => x.ThoiGianKy, (DateTime?)ban.SignedAt)
                    .SetProperty(x => x.DauThoiGian, dauThoiGian)
                    .SetProperty(x => x.ChuKyHopLe, (bool?)kiem.HopLe)
                    .SetProperty(x => x.LyDoChuKy, kiem.LyDo)
                    .SetProperty(x => x.LyDoLoi, (string?)null)
                    .SetProperty(x => x.ModifiedDate, now));

            await CongDonKetQuaAsync(db, ban.Viec.LoKyId, true);
        }

        public async Task GhiKetQuaLoiAsync(ViecKyDto viec, string lyDo, KiemChuKyDto? kiem)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();
            var now = DateTimeConstants.VietnamNow;

            if (kiem == null)
            {
                await db.LoKyFile
                    .Where(x => x.Id == viec.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.TrangThai, TrangThaiFileKy.Loi)
                        .SetProperty(x => x.LyDoLoi, lyDo)
                        .SetProperty(x => x.ModifiedDate, now));
            }
            else
            {
                await db.LoKyFile
                    .Where(x => x.Id == viec.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.TrangThai, TrangThaiFileKy.Loi)
                        .SetProperty(x => x.LyDoLoi, lyDo)
                        .SetProperty(x => x.ChuKyHopLe, (bool?)kiem.HopLe)
                        .SetProperty(x => x.LyDoChuKy, kiem.LyDo)
                        .SetProperty(x => x.ModifiedDate, now));
            }

            await CongDonKetQuaAsync(db, viec.LoKyId, false);
        }

        public async Task TraViecDangKyAsync(int loKyId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();
            var now = DateTimeConstants.VietnamNow;

            await db.LoKyFile
                .Where(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.DangKy)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.TrangThai, TrangThaiFileKy.Cho)
                    .SetProperty(x => x.ModifiedDate, now));
        }

        public void GhiNhatKyThoiGian(int thuTu, long msTong, long msTai, long msDung, long msKy, long msTsa)
        {
            _logger.LogInformation(
                "Ky file {ThuTu}: tong {Tong}ms (tai {Tai}ms, dung {Dung}ms, cho chu ky {Ky}ms, tsa {Tsa}ms, day len kho {Day}ms)",
                thuTu, msTong, msTai, msDung, msKy, msTsa, msTong - msTai - msDung - msKy - msTsa);
        }

        public async Task<byte[]?> TaiAnhDauDoAsync(TemplateEntity template,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(template.AnhDauDoObjectKey))
            {
                return null;
            }

            try
            {
                // Ảnh của template luôn nằm ở kho mặc định của service, không phải kho của dự án.
                return await _s3FileStorage.DownloadAsync(template.AnhDauDoObjectKey, cancellationToken);
            }
            catch (Exception ex)
            {
                // DỪNG cả lô, cùng lý do như ảnh chữ ký tươi: cả nghìn tờ thiếu con dấu chỉ lộ ra sau khi đã
                // ký xong thì phải ký lại toàn bộ.
                _logger.LogError(ex, "Không tải được ảnh dấu đỏ {Key} của template {TemplateId}",
                    template.AnhDauDoObjectKey, template.Id);

                throw new UserFriendlyException(ErrorCodes.TemplateImageInvalid,
                    $"Không tải được ảnh dấu đỏ của template \"{template.TenTemplate}\" từ kho "
                    + $"(object key: {template.AnhDauDoObjectKey}). Vào màn Template tải lại ảnh rồi ký lại.");
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
            string tenNguoiKy, byte[]? anhDauDo, byte[]? anhChuKyTuoi)
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
                AnhDauDo = anhDauDo,
                AnhChuKyTuoi = anhChuKyTuoi,
                DoDamDauDo = template.DoDamDauDo,
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
                AnhDauDo = mau.AnhDauDo,
                AnhChuKyTuoi = mau.AnhChuKyTuoi,
                DoDamDauDo = mau.DoDamDauDo,
                DoDamChuKyTuoi = mau.DoDamChuKyTuoi,
                DoDayNetChuKyTuoi = mau.DoDayNetChuKyTuoi,
                MauChuKySo = mau.MauChuKySo,
                MauChuKyTuoi = mau.MauChuKyTuoi,
                ViTri = mau.ViTri,
            };
        }

        public async Task CongDonKetQuaAsync(KssmDbContext db, int loKyId, bool thanhCong)
        {
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
        /// Xoá cả thư mục làm việc của lô trên kho mặc định: bản đã ký nằm trên Google Drive nên MinIO chỉ
        /// còn là chỗ tạm trú của bản nguồn tải lên. Lô của dự án KHÔNG dọn gì — bản nguồn ở đó là tài liệu
        /// thật của dự án, luồng ký chỉ đọc chứ không sở hữu chúng.
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
                    LoKyConstants.GetLoPrefix(loKyId), cancellationToken);
            }
            catch (Exception ex)
            {
                // Dọn hỏng không được phép làm lô đã ký xong bị coi là lỗi: file đã ký vẫn nguyên vẹn.
                _logger.LogWarning(ex, "Dọn thư mục lô {LoKyId} trên kho thất bại", loKyId);
            }
        }
    }
}
