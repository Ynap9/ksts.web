using AutoMapper;
using kssm.be.applications.Base;
using kssm.be.applications.LoKy.Dtos;
using kssm.be.applications.LoKy.Interfaces;
using kssm.be.external.Certificates.Interfaces;
using kssm.be.external.Mongo.Interfaces;
using kssm.be.external.S3.Dtos;
using kssm.be.external.S3.Interfaces;
using kssm.be.external.Signing.Dtos;
using kssm.be.external.Signing.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants;
using kssm.be.shared.Constants.LoKy;
using kssm.be.shared.Constants.SaoMai;
using kssm.be.shared.Constants.Signing;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using LoKyEntity = kssm.be.domain.LoKy.LoKy;
using LoKyFileEntity = kssm.be.domain.LoKy.LoKyFile;

namespace kssm.be.applications.LoKy.Implements
{
    public class LoKyService : BaseService, ILoKyService
    {
        private readonly ILoKyFileStorage _loKyFileStorage;
        private readonly IS3ClientFactory _s3ClientFactory;
        private readonly IProjectReader _projectReader;
        private readonly IKySoRunner _kySoRunner;
        private readonly IHangDoiKy _hangDoiKy;
        private readonly ICertificateTrustValidator _certificateTrustValidator;

        public LoKyService(
            KssmDbContext kstsDbContext,
            ILogger<LoKyService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILoKyFileStorage loKyFileStorage,
            IS3ClientFactory s3ClientFactory,
            IProjectReader projectReader,
            IKySoRunner kySoRunner,
            IHangDoiKy hangDoiKy,
            ICertificateTrustValidator certificateTrustValidator)
            : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _loKyFileStorage = loKyFileStorage;
            _s3ClientFactory = s3ClientFactory;
            _projectReader = projectReader;
            _kySoRunner = kySoRunner;
            _hangDoiKy = hangDoiKy;
            _certificateTrustValidator = certificateTrustValidator;
        }

        public async Task<List<ViewProjectDto>> DanhSachDuAnAsync(CancellationToken cancellationToken = default)
        {
            var duAn = await _projectReader.GetSignableProjectsAsync(cancellationToken);

            return duAn.Select(x => new ViewProjectDto
            {
                Id = x.Id,
                Ten = x.Name,
                Ma = x.Code,
            }).ToList();
        }

        public async Task<ViewLoKyDto> TaoLoAsync(TaoLoKyDto input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("{Method} templateId={TemplateId} projectId={ProjectId}",
                nameof(TaoLoAsync), input.TemplateId, input.ProjectId);

            var template = await _kstsDbContext.Template
                .FirstOrDefaultAsync(x => x.Id == input.TemplateId && !x.Deleted, cancellationToken)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    "Không tìm thấy template chữ ký đã chọn.");

            if (string.IsNullOrWhiteSpace(template.Thumbprint))
            {
                throw new UserFriendlyException(ErrorCodes.CertificateNotFound,
                    "Template chưa cấu hình chứng thư số nên chưa ký được.");
            }

            var lo = new LoKyEntity
            {
                NguoiTaoId = input.NguoiTaoId,
                TemplateId = template.Id,
                TrangThai = TrangThaiLoKy.MoiTao,
                TaiToken = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(LoKyConstants.DownloadTokenBytes)),
                CreatedDate = DateTimeConstants.VietnamNow,
            };

            _kstsDbContext.LoKy.Add(lo);
            await _kstsDbContext.SaveChangesAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(input.ProjectId))
            {
                // Lô nhận file tải lên: chỗ ghi bản ký nằm trong chính thư mục làm việc của lô.
                lo.TienToKho = LoKyConstants.GetSignedPrefix(lo.Id);
            }
            else
            {
                await NapFileDuAnAsync(lo, input.ProjectId, cancellationToken);
            }

            lo.ModifiedDate = DateTimeConstants.VietnamNow;
            await _kstsDbContext.SaveChangesAsync(cancellationToken);

            return ToViewDto(lo);
        }

        public async Task<ViewLoKyDto> ThemFileAsync(int loKyId, IFormFileCollection files,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("{Method} loKyId={LoKyId} soFile={SoFile}", nameof(ThemFileAsync),
                loKyId, files.Count);

            var lo = await LayLoAsync(loKyId);

            if (!string.IsNullOrWhiteSpace(lo.ProjectId))
            {
                throw new UserFriendlyException(ErrorCodes.LoKyDangChay,
                    "Lô này ký file của dự án nên không nhận file tải lên.");
            }

            if (lo.TrangThai != TrangThaiLoKy.MoiTao)
            {
                throw new UserFriendlyException(ErrorCodes.LoKyDangChay,
                    "Lô đã bắt đầu ký nên không nhận thêm file được.");
            }

            if (files.Count > LoKyConstants.MaxFilesPerBatch)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest,
                    $"Một đợt chỉ nhận tối đa {LoKyConstants.MaxFilesPerBatch} file.");
            }

            // Khử trùng theo tên file trong phạm vi lô: bên gọi gửi lại đúng đợt hỏng, gửi lại không được
            // nhân đôi số file.
            var tenDaCo = (await _kstsDbContext.LoKyFile
                    .Where(x => x.LoKyId == loKyId && !x.Deleted)
                    .Select(x => x.TenFile)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var kho = _s3ClientFactory.DefaultConnection();
            var thuTu = lo.TongSo;
            var now = DateTimeConstants.VietnamNow;

            foreach (var file in files)
            {
                if (file.Length == 0
                    || !file.FileName.EndsWith(LoKyConstants.PdfExtension, StringComparison.OrdinalIgnoreCase)
                    || !tenDaCo.Add(file.FileName))
                {
                    continue;
                }

                thuTu++;
                var objectKey = LoKyConstants.GetSourceObjectKey(loKyId, thuTu);
                await _loKyFileStorage.SaveSourceAsync(kho, file, objectKey, cancellationToken);

                _kstsDbContext.LoKyFile.Add(new LoKyFileEntity
                {
                    LoKyId = loKyId,
                    ThuTu = thuTu,
                    TenFile = file.FileName,
                    ObjectKeyNguon = objectKey,
                    TrangThai = TrangThaiFileKy.Cho,
                    CreatedDate = now,
                });
            }

            lo.TongSo = thuTu;
            lo.ModifiedDate = now;
            await _kstsDbContext.SaveChangesAsync(cancellationToken);

            return ToViewDto(lo);
        }

        public async Task<ViewLoKyDto> MoPhienKyAsync(int loKyId, MoPhienKyDto input)
        {
            _logger.LogInformation("{Method} loKyId={LoKyId}", nameof(MoPhienKyAsync), loKyId);

            var lo = await LayLoAsync(loKyId);

            if (string.IsNullOrWhiteSpace(input.ChungThuBase64))
            {
                throw new UserFriendlyException(ErrorCodes.CertificateNotFound,
                    "Máy người dùng chưa gửi chứng thư số của phiên ký.");
            }

            X509Certificate2 cert;
            try
            {
                cert = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(input.ChungThuBase64));
            }
            catch (Exception)
            {
                throw new UserFriendlyException(ErrorCodes.CertificateNotFound,
                    "Không đọc được chứng thư số máy người dùng gửi lên.");
            }

            // Máy chủ TỰ thẩm định chuỗi tin cậy: máy người dùng không kiểm soát được nên cờ tin cậy nó gửi
            // lên là vô giá trị.
            if (!_certificateTrustValidator.IsTrusted(cert, DateTime.UtcNow))
            {
                throw new UserFriendlyException(ErrorCodes.CertificateCannotSign,
                    "Chứng thư số không dựng được chuỗi tin cậy về CA đã ghim.");
            }

            _hangDoiKy.MoPhien(loKyId, cert);

            lo.Thumbprint = cert.Thumbprint;
            lo.ModifiedDate = DateTimeConstants.VietnamNow;
            await _kstsDbContext.SaveChangesAsync();

            return ToViewDto(lo);
        }

        public async Task DongPhienKyAsync(int loKyId)
        {
            await LayLoAsync(loKyId);
            _hangDoiKy.DongPhien(loKyId);
        }

        public async Task<ViewLoKyDto> BatDauAsync(int loKyId, BatDauKyDto input)
        {
            _logger.LogInformation("{Method} loKyId={LoKyId}", nameof(BatDauAsync), loKyId);

            var lo = await LayLoAsync(loKyId);

            if (string.IsNullOrWhiteSpace(input.Thumbprint))
            {
                throw new UserFriendlyException(ErrorCodes.CertificateNotFound,
                    "Chưa chọn chứng thư số để ký.");
            }

            if (_kySoRunner.DangChay(loKyId))
            {
                throw new UserFriendlyException(ErrorCodes.LoKyDangChay, "Lô này đang được ký.");
            }

            if (!_hangDoiKy.PhienConSong(loKyId))
            {
                throw new UserFriendlyException(ErrorCodes.LoKyPhienDaMat,
                    "Chưa mở phiên ký với máy người dùng, hoặc phiên đã mất. Mở lại phiên rồi ký tiếp.");
            }

            // Ký tiếp phải ký lại cả file hỏng của lượt trước: phần lớn lỗi là do phiên ký đứt giữa chừng
            // (rút token, đóng tab, tạm dừng) chứ không phải file hỏng, để nguyên thì chúng không bao giờ
            // được ký. Trừ lại bộ đếm đúng bấy nhiêu vì nó cộng dồn theo từng file.
            var soLoiTraLai = await TraFileLoiVeHangDoiAsync(loKyId);
            if (soLoiTraLai > 0)
            {
                lo.SoLoi = Math.Max(0, lo.SoLoi - soLoiTraLai);
            }

            // Ký tiếp cũng đi đúng đường này: việc lấy file luôn lọc theo trạng thái chờ nên hết file chờ
            // nghĩa là không còn gì để ký, kể cả khi lô từng dừng giữa chừng.
            var conCho = await _kstsDbContext.LoKyFile
                .AnyAsync(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.Cho);

            if (!conCho)
            {
                throw new UserFriendlyException(ErrorCodes.LoKyRong,
                    lo.TongSo == 0 ? "Lô chưa có file nào để ký." : "Lô không còn file nào đang chờ ký.");
            }

            lo.Thumbprint = input.Thumbprint;
            lo.TrangThai = TrangThaiLoKy.DangKy;
            lo.LoiChung = null;
            lo.ThoiDiemBatDau ??= DateTimeConstants.VietnamNow;
            lo.ModifiedDate = DateTimeConstants.VietnamNow;
            await _kstsDbContext.SaveChangesAsync();

            _kySoRunner.BatDau(loKyId, input.Thumbprint);

            return ToViewDto(lo);
        }

        public async Task<List<YeuCauKyDto>> LayYeuCauKyAsync(int loKyId,
            CancellationToken cancellationToken)
        {
            await LayLoAsync(loKyId);

            return await _hangDoiKy.LayYeuCauAsync(loKyId,
                TimeSpan.FromSeconds(SigningQueueConstants.GiayChoLayViec), cancellationToken);
        }

        public async Task NopChuKyAsync(int loKyId, List<NopChuKyDto> input)
        {
            await LayLoAsync(loKyId);

            var phienDaMat = _hangDoiKy.NopKetQua(loKyId, input.Select(x => new KetQuaKyDto
            {
                YeuCauId = x.YeuCauId,
                ChuKyBase64 = x.ChuKyBase64,
                Loi = x.Loi,
                PhienDaMat = x.PhienDaMat,
            }));

            if (!phienDaMat)
            {
                return;
            }

            // Phiên mất giữa lô KHÔNG được biến thành một loạt file lỗi: dừng lô lại, trả file dở về hàng
            // đợi rồi để người dùng mở phiên mới và bấm ký tiếp.
            await TamDungAsync(loKyId, new TamDungKyDto
            {
                LyDo = "Phiên ký ở máy người dùng đã mất. Mở lại phiên rồi bấm ký tiếp.",
            });
        }

        public async Task<ViewLoKyDto> TamDungAsync(int loKyId, TamDungKyDto input)
        {
            _logger.LogInformation("{Method} loKyId={LoKyId}", nameof(TamDungAsync), loKyId);

            var lo = await LayLoAsync(loKyId);

            _kySoRunner.Dung(loKyId);
            _hangDoiKy.DongPhien(loKyId);
            await TraFileDangKyVeHangDoiAsync(loKyId);

            lo.TrangThai = TrangThaiLoKy.TamDung;
            lo.LoiChung = input.LyDo;
            lo.ModifiedDate = DateTimeConstants.VietnamNow;
            await _kstsDbContext.SaveChangesAsync();

            return ToViewDto(lo);
        }

        public async Task<ViewLoKyDto> HuyAsync(int loKyId)
        {
            _logger.LogInformation("{Method} loKyId={LoKyId}", nameof(HuyAsync), loKyId);

            var lo = await LayLoAsync(loKyId);

            _kySoRunner.Dung(loKyId);
            _hangDoiKy.DongPhien(loKyId);
            await TraFileDangKyVeHangDoiAsync(loKyId);

            lo.TrangThai = TrangThaiLoKy.Huy;
            lo.ThoiDiemXong = DateTimeConstants.VietnamNow;
            lo.ModifiedDate = lo.ThoiDiemXong;
            await _kstsDbContext.SaveChangesAsync();

            return ToViewDto(lo);
        }

        public async Task<List<ViewFileKyDto>> DanhSachFileAsync(int loKyId)
        {
            await LayLoAsync(loKyId);

            return (await _kstsDbContext.LoKyFile
                    .Where(x => x.LoKyId == loKyId && !x.Deleted)
                    .OrderBy(x => x.ThuTu)
                    .AsNoTracking()
                    .ToListAsync())
                .Select(ToViewFileDto)
                .ToList();
        }

        public async Task<ViewTienDoDto> TrangThaiAsync(int loKyId)
        {
            var lo = await LayLoAsync(loKyId);

            // CHỈ lấy file lỗi: bên gọi hỏi tiến độ mỗi vài giây, kèm cả nghìn dòng mỗi nhịp là thứ làm
            // trình duyệt cạn tài nguyên rồi chết giữa lô.
            var filesLoi = (await _kstsDbContext.LoKyFile
                    .Where(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.Loi)
                    .OrderBy(x => x.ThuTu)
                    .AsNoTracking()
                    .ToListAsync())
                .Select(ToViewFileDto)
                .ToList();

            // Kèm những file VỪA ký xong để bảng điền dần thời gian ký và dấu thời gian.
            var vuaXong = (await _kstsDbContext.LoKyFile
                    .Where(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.Xong)
                    .OrderByDescending(x => x.ModifiedDate)
                    .Take(LoKyConstants.RecentDonePerPoll)
                    .AsNoTracking()
                    .ToListAsync())
                .Select(ToViewFileDto)
                .ToList();

            return new ViewTienDoDto
            {
                Id = lo.Id,
                TrangThai = lo.TrangThai.ToString(),
                TaiToken = lo.TaiToken,
                TongSo = lo.TongSo,
                DaXong = lo.DaXong,
                SoLoi = lo.SoLoi,
                DangChay = _kySoRunner.DangChay(loKyId),
                HoanTat = lo.TrangThai is TrangThaiLoKy.Xong or TrangThaiLoKy.Huy or TrangThaiLoKy.Loi,
                PhienConSong = _hangDoiKy.PhienConSong(loKyId),
                LoiChung = lo.LoiChung,
                TienToKho = lo.TienToKho,
                FilesLoi = filesLoi,
                FilesVuaXong = vuaXong,
            };
        }

        public async Task<ViewLoKyDto?> LoDangChayAsync(string? nguoiTaoId)
        {
            var lo = await _kstsDbContext.LoKy
                .Where(x => !x.Deleted
                    && (x.TrangThai == TrangThaiLoKy.DangKy || x.TrangThai == TrangThaiLoKy.TamDung)
                    && (nguoiTaoId == null || x.NguoiTaoId == nguoiTaoId))
                .OrderByDescending(x => x.Id)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return lo == null ? null : ToViewDto(lo);
        }

        public async Task GhiNenAsync(int loKyId, string taiToken, Stream dich,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Method} loKyId={LoKyId}", nameof(GhiNenAsync), loKyId);

            // So khớp bằng thời gian cố định: token là bí mật duy nhất chặn đường tải này, so sánh chuỗi
            // thường sẽ rò rỉ độ dài tiền tố khớp qua thời gian trả lời.
            var lo = await _kstsDbContext.LoKy.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == loKyId && !x.Deleted, cancellationToken);

            if (lo == null || string.IsNullOrEmpty(taiToken) || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(lo.TaiToken), Encoding.UTF8.GetBytes(taiToken)))
            {
                throw new UserFriendlyException(ErrorCodes.LoKyNotFound, "Không tìm thấy lô ký.");
            }

            if (_kySoRunner.DangChay(loKyId))
            {
                throw new UserFriendlyException(ErrorCodes.LoKyDangChay,
                    "Lô đang ký dở nên chưa đóng gói được.");
            }

            // Tiền tố rỗng thì lệnh liệt kê sẽ quét TOÀN BỘ bucket — với lô của dự án là gói cả kho tài liệu
            // của họ vào zip. Chặn ở đây chứ không để lọt xuống tầng kho.
            if (string.IsNullOrWhiteSpace(lo.TienToKho))
            {
                throw new UserFriendlyException(ErrorCodes.LoKyRong, "Lô chưa có file nào ký xong.");
            }

            var kho = await LayKhoAsync(lo.ProjectId, cancellationToken);

            // Đọc thẳng danh sách trên kho, KHÔNG ghép lại object key từ bảng: key có thật là key đã ghi lúc
            // ký, còn ghép lại từ tên file có dấu và khoảng trắng thì lệch một ký tự là tải hụt, mà nhánh
            // tải hụt lại bỏ qua im lặng nên file biến mất khỏi gói không dấu vết.
            var keys = await _loKyFileStorage.ListKeysAsync(kho, lo.TienToKho, cancellationToken);

            if (keys.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.LoKyRong, "Lô chưa có file nào ký xong.");
            }

            using var archive = new ZipArchive(dich, ZipArchiveMode.Create, leaveOpen: true);

            // Tên đi kèm luôn với việc tải của chính nó: file hỏng thì bỏ qua, mà đếm thứ tự riêng sẽ lệch
            // khỏi danh sách ngay từ file hỏng đầu tiên và mọi file sau đó vào gói sai tên.
            var hangCho = new Queue<(string Ten, Task<byte[]?> Tai)>();
            var ke = 0;

            while (ke < keys.Count && hangCho.Count < LoKyConstants.PrefetchWhenZipping)
            {
                hangCho.Enqueue((TenTrongGoi(keys[ke]), TaiMotFileAsync(kho, keys[ke], cancellationToken)));
                ke++;
            }

            while (hangCho.Count > 0)
            {
                var (ten, tai) = hangCho.Dequeue();
                var noiDung = await tai;

                if (ke < keys.Count)
                {
                    hangCho.Enqueue((TenTrongGoi(keys[ke]), TaiMotFileAsync(kho, keys[ke], cancellationToken)));
                    ke++;
                }

                if (noiDung == null)
                {
                    continue;
                }

                var entry = archive.CreateEntry(ten, CompressionLevel.NoCompression);
                await using var stream = entry.Open();
                await stream.WriteAsync(noiDung, cancellationToken);
            }
        }

        /// <summary>Tên trong gói là mẩu cuối của object key: gói để phẳng, không dựng lại cây thư mục của kho.</summary>
        public string TenTrongGoi(string objectKey) => objectKey[(objectKey.LastIndexOf('/') + 1)..];

        /// <summary>
        /// Nạp toàn bộ file PDF của dự án vào lô. Bản nguồn giữ nguyên tại chỗ trên kho của dự án — lô chỉ
        /// ghi lại object key, không chép byte nào sang nơi khác.
        /// </summary>
        public async Task NapFileDuAnAsync(LoKyEntity lo, string projectId,
            CancellationToken cancellationToken)
        {
            var duAn = await _projectReader.GetProjectAsync(projectId, cancellationToken);

            if (!SaoMaiConstants.SignableStatuses.Any(x =>
                string.Equals(x, duAn.Status, StringComparison.Ordinal)))
            {
                throw new UserFriendlyException(ErrorCodes.DuAnSaiTrangThai,
                    $"Dự án \"{duAn.Name}\" không ở trạng thái đang nghiệm thu hoặc đã xong nên chưa ký số được.");
            }

            var kho = await _projectReader.GetProjectStorageAsync(projectId, cancellationToken)
                ?? _s3ClientFactory.DefaultConnection();
            var files = await _projectReader.GetPdfFilesAsync(projectId, kho, cancellationToken);

            if (files.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.LoKyThuMucKhoRong,
                    $"Dự án \"{duAn.Name}\" không có file PDF nào để ký.");
            }

            var now = DateTimeConstants.VietnamNow;
            var tenDaCo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var thuTu = 0;

            foreach (var file in files)
            {
                thuTu++;

                _kstsDbContext.LoKyFile.Add(new LoKyFileEntity
                {
                    LoKyId = lo.Id,
                    ThuTu = thuTu,
                    DocumentId = file.Id,
                    TenFile = TenFileKhongTrung(file.FileName, thuTu, tenDaCo),
                    ObjectKeyNguon = file.ObjectKey,
                    TrangThai = TrangThaiFileKy.Cho,
                    CreatedDate = now,
                });
            }

            lo.ProjectId = duAn.Id;
            lo.TenDuAn = duAn.Name;
            lo.TongSo = thuTu;
            lo.TienToKho = LoKyConstants.GetProjectSignedPrefix(duAn.Name);
        }

        /// <summary>
        /// Hai tài liệu của cùng một dự án được phép trùng tên file. Bản ký ghi theo tên nên phải tách ra,
        /// nếu không file sau ghi đè bản ký của file trước và lô mất bớt kết quả mà không báo gì.
        /// </summary>
        public string TenFileKhongTrung(string tenFile, int thuTu, HashSet<string> tenDaCo)
        {
            if (tenDaCo.Add(tenFile))
            {
                return tenFile;
            }

            var duoi = Path.GetExtension(tenFile);
            var ten = Path.GetFileNameWithoutExtension(tenFile);
            var tenMoi = $"{ten}-{thuTu}{duoi}";
            tenDaCo.Add(tenMoi);

            return tenMoi;
        }

        /// <summary>
        /// Trả mọi file hỏng về hàng đợi, trả về số file đã gỡ để bộ đếm lỗi của lô trừ đi đúng bấy nhiêu.
        /// Kết quả kiểm chữ ký của lượt trước xoá theo: nó thuộc về lần thử đã hỏng, không phải lần sắp chạy.
        /// </summary>
        public async Task<int> TraFileLoiVeHangDoiAsync(int loKyId)
        {
            var now = DateTimeConstants.VietnamNow;

            return await _kstsDbContext.LoKyFile
                .Where(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.Loi)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.TrangThai, TrangThaiFileKy.Cho)
                    .SetProperty(x => x.LyDoLoi, (string?)null)
                    .SetProperty(x => x.ChuKyHopLe, (bool?)null)
                    .SetProperty(x => x.LyDoChuKy, (string?)null)
                    .SetProperty(x => x.ModifiedDate, now));
        }

        /// <summary>
        /// Trả mọi file đang ký dở về hàng đợi. Gọi khi lô dừng lại: file đó chưa ký xong nhưng cũng không
        /// hỏng, để nguyên trạng thái đang ký là lần chạy sau bỏ sót nó.
        /// </summary>
        public async Task TraFileDangKyVeHangDoiAsync(int loKyId)
        {
            var now = DateTimeConstants.VietnamNow;

            await _kstsDbContext.LoKyFile
                .Where(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.DangKy)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.TrangThai, TrangThaiFileKy.Cho)
                    .SetProperty(x => x.ModifiedDate, now));
        }

        /// <summary>
        /// Kho của lô: MinIO riêng của dự án; kho mặc định cho lô nhận file tải lên và cho dự án chưa khai
        /// đủ khoá MinIO.
        /// </summary>
        public async Task<S3ConnectionDto> LayKhoAsync(string? projectId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                return _s3ClientFactory.DefaultConnection();
            }

            return await _projectReader.GetProjectStorageAsync(projectId, cancellationToken)
                ?? _s3ClientFactory.DefaultConnection();
        }

        public async Task<byte[]?> TaiMotFileAsync(S3ConnectionDto kho, string objectKey,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _loKyFileStorage.DownloadAsync(kho, objectKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kéo {ObjectKey} từ kho về để nén hỏng", objectKey);
                return null;
            }
        }

        public async Task<LoKyEntity> LayLoAsync(int loKyId)
        {
            return await _kstsDbContext.LoKy.FirstOrDefaultAsync(x => x.Id == loKyId && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.LoKyNotFound, "Không tìm thấy lô ký.");
        }

        public string MaTrangThai(TrangThaiFileKy trangThai) => trangThai switch
        {
            TrangThaiFileKy.DangKy => LoKyConstants.StatusSigning,
            TrangThaiFileKy.Xong => LoKyConstants.StatusDone,
            TrangThaiFileKy.Loi => LoKyConstants.StatusFailed,
            _ => LoKyConstants.StatusPending,
        };

        public ViewFileKyDto ToViewFileDto(LoKyFileEntity file) => new()
        {
            Id = file.Id,
            ThuTu = file.ThuTu,
            DocumentId = file.DocumentId,
            TenFile = file.TenFile,
            TrangThai = MaTrangThai(file.TrangThai),
            LyDoLoi = file.LyDoLoi,
            ThoiGianKy = file.ThoiGianKy,
            DauThoiGian = file.DauThoiGian,
            ChuKyHopLe = file.ChuKyHopLe,
            LyDoChuKy = file.LyDoChuKy,
        };

        public ViewLoKyDto ToViewDto(LoKyEntity lo) => new()
        {
            Id = lo.Id,
            TemplateId = lo.TemplateId,
            ProjectId = lo.ProjectId,
            TenDuAn = lo.TenDuAn,
            Thumbprint = lo.Thumbprint,
            TaiToken = lo.TaiToken,
            TrangThai = lo.TrangThai.ToString(),
            TongSo = lo.TongSo,
            DaXong = lo.DaXong,
            SoLoi = lo.SoLoi,
            TienToKho = lo.TienToKho,
            CreatedDate = lo.CreatedDate,
        };
    }
}
