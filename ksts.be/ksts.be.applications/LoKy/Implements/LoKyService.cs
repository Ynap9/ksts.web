using AutoMapper;
using ksts.be.applications.Base;
using ksts.be.applications.LoKy.Dtos;
using ksts.be.applications.LoKy.Interfaces;
using ksts.be.external.Storage.Interfaces;
using ksts.be.infrastructure.Persistence;
using ksts.be.shared.Constants.LoKy;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using ksts.be.shared.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using LoKyEntity = ksts.be.domain.LoKy.LoKy;
using LoKyFileEntity = ksts.be.domain.LoKy.LoKyFile;

namespace ksts.be.applications.LoKy.Implements
{
    /// <inheritdoc/>
    public class LoKyService : BaseService, ILoKyService
    {
        private readonly ILoKyFileStorage _loKyFileStorage;
        private readonly IS3FileStorage _s3FileStorage;
        private readonly IKySoRunner _kySoRunner;
        private readonly IDayLenKhoRunner _dayLenKhoRunner;
        private readonly S3Settings _s3Settings;

        public LoKyService(
            KstsDbContext kstsDbContext,
            ILogger<LoKyService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILoKyFileStorage loKyFileStorage,
            IS3FileStorage s3FileStorage,
            IKySoRunner kySoRunner,
            IDayLenKhoRunner dayLenKhoRunner,
            IOptions<S3Settings> s3Options) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _loKyFileStorage = loKyFileStorage;
            _s3FileStorage = s3FileStorage;
            _kySoRunner = kySoRunner;
            _dayLenKhoRunner = dayLenKhoRunner;
            _s3Settings = s3Options.Value;
        }

        /// <inheritdoc/>
        public async Task<ViewLoKyDto> TaoLoAsync(TaoLoKyDto input)
        {
            _logger.LogInformation($"{nameof(TaoLoAsync)} templateId={input.TemplateId}");

            var userId = getCurrentUserId();
            var template = await _kstsDbContext.Template
                .FirstOrDefaultAsync(x => x.Id == input.TemplateId && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.TemplateNotFound,
                    "Không tìm thấy template chữ ký đã chọn.");

            if (template.IdUser != userId && !IsSuperAdmin())
            {
                throw new UserFriendlyException(ErrorCodes.TemplateAccessDenied,
                    "Template này thuộc về người dùng khác.");
            }

            var lo = new LoKyEntity
            {
                IdUser = userId,
                TemplateId = template.Id,
                TrangThai = TrangThaiLoKy.MoiTao,
                TaiToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(LoKyConstants.SoByteTaiToken)),
                CreatedBy = getCurrentName(),
                CreatedDate = GetVietnamTime(),
            };

            _kstsDbContext.LoKy.Add(lo);
            await _kstsDbContext.SaveChangesAsync();

            return ToViewDto(lo);
        }

        /// <inheritdoc/>
        public async Task<ViewLoKyDto> ThemFileAsync(int loKyId, IFormFileCollection files)
        {
            _logger.LogInformation($"{nameof(ThemFileAsync)} loKyId={loKyId} soFile={files.Count}");

            var lo = await LayLoAsync(loKyId);
            if (lo.TrangThai != TrangThaiLoKy.MoiTao)
            {
                throw new UserFriendlyException(ErrorCodes.LoKyDangChay,
                    "Lô đã bắt đầu ký nên không nhận thêm file được.");
            }

            if (files.Count > LoKyConstants.SoFileToiDaMoiDot)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest,
                    $"Một đợt chỉ nhận tối đa {LoKyConstants.SoFileToiDaMoiDot} file.");
            }

            // Khử trùng theo tên file trong phạm vi lô: FE gửi lại đúng đợt hỏng, gửi lại không được nhân đôi.
            var daCo = await _kstsDbContext.LoKyFile
                .Where(x => x.LoKyId == loKyId && !x.Deleted)
                .Select(x => x.TenFile)
                .ToListAsync();
            var tenDaCo = daCo.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var thuTu = lo.TongSo;
            var now = GetVietnamTime();

            foreach (var file in files)
            {
                if (file.Length == 0
                    || !file.FileName.EndsWith(LoKyConstants.PhanMoRongPdf, StringComparison.OrdinalIgnoreCase)
                    || !tenDaCo.Add(file.FileName))
                {
                    continue;
                }

                thuTu++;
                var objectKey = await _loKyFileStorage.LuuFileNguonAsync(loKyId, thuTu, file);

                _kstsDbContext.LoKyFile.Add(new LoKyFileEntity
                {
                    LoKyId = loKyId,
                    ThuTu = thuTu,
                    TenFile = file.FileName,
                    ObjectKeyNguon = objectKey,
                    TrangThai = TrangThaiFileKy.Cho,
                    CreatedBy = getCurrentName(),
                    CreatedDate = now,
                });
            }

            lo.TongSo = thuTu;
            lo.ModifiedDate = now;
            await _kstsDbContext.SaveChangesAsync();

            return ToViewDto(lo);
        }

        /// <inheritdoc/>
        public async Task<ViewLoKyDto> ThemFileTuKhoAsync(int loKyId, ThemFileTuKhoDto input)
        {
            _logger.LogInformation($"{nameof(ThemFileTuKhoAsync)} loKyId={loKyId} duongDan={input.DuongDan}");

            var lo = await LayLoAsync(loKyId);
            if (lo.TrangThai != TrangThaiLoKy.MoiTao)
            {
                throw new UserFriendlyException(ErrorCodes.LoKyDangChay,
                    "Lô đã bắt đầu ký nên không nhận thêm file được.");
            }

            var tienTo = ChuanHoaTienTo(input.DuongDan);
            if (string.IsNullOrEmpty(tienTo))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest,
                    "Chưa nhập đường dẫn thư mục trong kho.");
            }

            var khoaTrenKho = (await _s3FileStorage.ListKeysAsync(tienTo))
                .Where(x => x.EndsWith(LoKyConstants.PhanMoRongPdf, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (khoaTrenKho.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.LoKyThuMucKhoRong,
                    $"Không thấy file PDF nào trong thư mục \"{tienTo}\" của kho.");
            }

            var daCo = await _kstsDbContext.LoKyFile
                .Where(x => x.LoKyId == loKyId && !x.Deleted)
                .Select(x => x.TenFile)
                .ToListAsync();
            var tenDaCo = daCo.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var thuTu = lo.TongSo;
            var now = GetVietnamTime();

            foreach (var objectKey in khoaTrenKho)
            {
                var tenFile = objectKey[(objectKey.LastIndexOf('/') + 1)..];
                if (!tenDaCo.Add(tenFile))
                {
                    continue;
                }

                thuTu++;

                // Trỏ THẲNG vào object đang có trên kho, không chép sang lo-ky/{id}/nguon/: chép là nhân đôi
                // dung lượng lưu trữ và tốn đúng ngần ấy thời gian, trong khi khâu ký chỉ cần một object key
                // để tải nội dung về.
                _kstsDbContext.LoKyFile.Add(new LoKyFileEntity
                {
                    LoKyId = loKyId,
                    ThuTu = thuTu,
                    TenFile = tenFile,
                    ObjectKeyNguon = objectKey,
                    TrangThai = TrangThaiFileKy.Cho,
                    CreatedBy = getCurrentName(),
                    CreatedDate = now,
                });
            }

            lo.TongSo = thuTu;
            lo.ModifiedDate = now;
            await _kstsDbContext.SaveChangesAsync();

            return ToViewDto(lo);
        }

        /// <inheritdoc/>
        public string ChuanHoaTienTo(string duongDan)
        {
            var giaTri = (duongDan ?? string.Empty).Trim();
            if (giaTri.Length == 0)
            {
                return string.Empty;
            }

            // Dán nguyên link từ trình duyệt kho thì phần host và tham số truy vấn đi kèm; chỉ giữ phần
            // đường dẫn. Giải mã %20 để tên thư mục có dấu cách khớp đúng với key thật.
            if (Uri.TryCreate(giaTri, UriKind.Absolute, out var uri))
            {
                giaTri = Uri.UnescapeDataString(uri.AbsolutePath);
            }

            giaTri = giaTri.Replace('\\', '/').Trim('/');

            // Đường dẫn trong bảng điều khiển của kho thường kèm tên bucket ở đầu, mà object key thì không.
            var bucket = _s3Settings.Bucket;
            if (!string.IsNullOrEmpty(bucket)
                && giaTri.StartsWith(bucket + "/", StringComparison.OrdinalIgnoreCase))
            {
                giaTri = giaTri[(bucket.Length + 1)..];
            }

            // "browser/{bucket}/{key}" là đường của trang quản trị MinIO, không phải object key. Đoạn ngay
            // sau "browser/" LUÔN là tên bucket nên cắt thẳng, không so với tên bucket trong cấu hình: dán
            // link của bucket khác thì so sẽ không khớp và tên bucket lọt vào tiền tố, tìm mãi không ra file.
            const string tienToTrinhDuyet = "browser/";
            if (giaTri.StartsWith(tienToTrinhDuyet, StringComparison.OrdinalIgnoreCase))
            {
                giaTri = giaTri[tienToTrinhDuyet.Length..];
                var vachSau = giaTri.IndexOf('/');
                giaTri = vachSau < 0 ? string.Empty : giaTri[(vachSau + 1)..];
            }

            giaTri = giaTri.Trim('/');
            return giaTri.Length == 0 ? string.Empty : giaTri + "/";
        }

        /// <inheritdoc/>
        public async Task<ViewLoKyDto> BatDauAsync(int loKyId, BatDauKyDto input)
        {
            _logger.LogInformation($"{nameof(BatDauAsync)} loKyId={loKyId}");

            var lo = await LayLoAsync(loKyId);
            if (lo.TongSo == 0)
            {
                throw new UserFriendlyException(ErrorCodes.LoKyRong, "Lô chưa có file nào để ký.");
            }

            if (string.IsNullOrWhiteSpace(input.Thumbprint))
            {
                throw new UserFriendlyException(ErrorCodes.CertificateNotFound,
                    "Chưa chọn chứng thư số để ký.");
            }

            if (_kySoRunner.DangChay(loKyId))
            {
                throw new UserFriendlyException(ErrorCodes.LoKyDangChay, "Lô này đang được ký.");
            }

            lo.Thumbprint = input.Thumbprint;
            lo.TrangThai = TrangThaiLoKy.DangKy;
            lo.LoiChung = null;
            lo.ThoiDiemBatDau = GetVietnamTime();
            lo.ModifiedDate = lo.ThoiDiemBatDau;
            await _kstsDbContext.SaveChangesAsync();

            _kySoRunner.BatDau(loKyId, input.Thumbprint);

            return ToViewDto(lo);
        }

        /// <inheritdoc/>
        public async Task<ViewLoKyDto> BatDauDayLenKhoAsync(int loKyId)
        {
            _logger.LogInformation($"{nameof(BatDauDayLenKhoAsync)} loKyId={loKyId}");

            var lo = await LayLoAsync(loKyId);

            if (_kySoRunner.DangChay(loKyId))
            {
                throw new UserFriendlyException(ErrorCodes.LoKyDangChay,
                    "Lô đang ký dở nên chưa đẩy lên kho được.");
            }

            // Hỏi RUNNER chứ không đọc cờ trong DB: cờ chỉ để hiển thị, mà API tắt giữa chừng thì nó kẹt lại
            // true vĩnh viễn và chặn luôn mọi lần đẩy lại về sau.
            if (_dayLenKhoRunner.DangChay(loKyId))
            {
                throw new UserFriendlyException(ErrorCodes.LoKyDangDayLenKho,
                    "Lô này đang được đẩy lên kho, chờ xong rồi hãy đẩy lại.");
            }

            var soXong = await _kstsDbContext.LoKyFile
                .CountAsync(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.Xong
                    && x.ObjectKeyDaKy != null);

            if (soXong == 0)
            {
                throw new UserFriendlyException(ErrorCodes.LoKyRong, "Lô chưa có file nào ký xong để đẩy.");
            }

            // Đặt lại bộ đếm để lần đẩy lại sau khi hỏng không cộng dồn lên số cũ.
            lo.DangDayLenKho = true;
            lo.HoanTatDayLenKho = false;
            lo.DaDayLenKho = 0;
            lo.SoLoiDayLenKho = 0;
            lo.LoiDayLenKho = null;
            lo.TienToKho = LoKyConstants.GetKhoDaKyPrefix();
            lo.ModifiedDate = GetVietnamTime();
            await _kstsDbContext.SaveChangesAsync();

            _dayLenKhoRunner.BatDau(loKyId);

            return ToViewDto(lo);
        }

        /// <inheritdoc/>
        public async Task<ViewTienDoDto> TrangThaiAsync(int loKyId)
        {
            var lo = await LayLoAsync(loKyId);

            // Lấy về rồi mới quy đổi trạng thái: MaTrangThai là hàm của mình, EF không dịch sang SQL được.
            var files = (await _kstsDbContext.LoKyFile
                    .Where(x => x.LoKyId == loKyId && !x.Deleted)
                    .OrderBy(x => x.ThuTu)
                    .AsNoTracking()
                    .ToListAsync())
                .Select(x => new ViewFileKyDto
                {
                    Id = x.Id,
                    ThuTu = x.ThuTu,
                    TenFile = x.TenFile,
                    TrangThai = MaTrangThai(x.TrangThai),
                    LyDoLoi = x.LyDoLoi,
                    ThoiGianKy = x.ThoiGianKy,
                    DauThoiGian = x.DauThoiGian,
                })
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
                LoiChung = lo.LoiChung,
                DangDayLenKho = _dayLenKhoRunner.DangChay(loKyId),
                DaDayLenKho = lo.DaDayLenKho,
                SoLoiDayLenKho = lo.SoLoiDayLenKho,
                HoanTatDayLenKho = lo.HoanTatDayLenKho,
                LoiDayLenKho = lo.LoiDayLenKho,
                TienToKho = lo.TienToKho,
                Files = files,
            };
        }

        /// <inheritdoc/>
        public async Task<ViewLoKyDto?> LoDangChayAsync()
        {
            var userId = getCurrentUserId();
            var lo = await _kstsDbContext.LoKy
                .Where(x => x.IdUser == userId && !x.Deleted && x.TrangThai == TrangThaiLoKy.DangKy)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            return lo == null ? null : ToViewDto(lo);
        }

        /// <inheritdoc/>
        public async Task HuyAsync(int loKyId)
        {
            _logger.LogInformation($"{nameof(HuyAsync)} loKyId={loKyId}");

            var lo = await LayLoAsync(loKyId);
            _kySoRunner.Dung(loKyId);

            lo.TrangThai = TrangThaiLoKy.Huy;
            lo.ModifiedDate = GetVietnamTime();
            await _kstsDbContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<Stream> TaiZipAsync(int loKyId, string taiToken)
        {
            _logger.LogInformation($"{nameof(TaiZipAsync)} loKyId={loKyId}");

            // So khớp bằng thời gian cố định: token là bí mật duy nhất chặn đường tải này, so sánh chuỗi
            // thường sẽ rò rỉ độ dài tiền tố khớp qua thời gian trả lời.
            var lo = await _kstsDbContext.LoKy.FirstOrDefaultAsync(x => x.Id == loKyId && !x.Deleted);
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

            var files = await _kstsDbContext.LoKyFile
                .Where(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.Xong
                    && x.ObjectKeyDaKy != null)
                .OrderBy(x => x.ThuTu)
                .ToListAsync();

            if (files.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.LoKyRong, "Lô chưa có file nào ký xong.");
            }

            // DeleteOnClose: file tạm tự biến mất khi stream đóng, kể cả khi request bị huỷ giữa chừng.
            var temp = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite,
                FileShare.None, 81920, FileOptions.DeleteOnClose);

            using (var archive = new ZipArchive(temp, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var file in files)
                {
                    var noiDung = await _loKyFileStorage.TaiAsync(file.ObjectKeyDaKy!);
                    var entry = archive.CreateEntry(file.TenFile, CompressionLevel.NoCompression);

                    await using var stream = entry.Open();
                    await stream.WriteAsync(noiDung);
                }
            }

            temp.Position = 0;
            return temp;
        }

        /// <inheritdoc/>
        public async Task<LoKyEntity> LayLoAsync(int loKyId)
        {
            var lo = await _kstsDbContext.LoKy.FirstOrDefaultAsync(x => x.Id == loKyId && !x.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.LoKyNotFound, "Không tìm thấy lô ký.");

            if (lo.IdUser != getCurrentUserId() && !IsSuperAdmin())
            {
                throw new UserFriendlyException(ErrorCodes.LoKyAccessDenied,
                    "Lô ký này thuộc về người dùng khác.");
            }

            return lo;
        }

        /// <inheritdoc/>
        public string MaTrangThai(TrangThaiFileKy trangThai) => trangThai switch
        {
            TrangThaiFileKy.DangKy => LoKyConstants.MaTrangThaiDangKy,
            TrangThaiFileKy.Xong => LoKyConstants.MaTrangThaiXong,
            TrangThaiFileKy.Loi => LoKyConstants.MaTrangThaiLoi,
            _ => LoKyConstants.MaTrangThaiCho,
        };

        /// <inheritdoc/>
        public ViewLoKyDto ToViewDto(LoKyEntity lo) => new()
        {
            Id = lo.Id,
            TemplateId = lo.TemplateId,
            Thumbprint = lo.Thumbprint,
            TaiToken = lo.TaiToken,
            TrangThai = lo.TrangThai.ToString(),
            TongSo = lo.TongSo,
            DaXong = lo.DaXong,
            SoLoi = lo.SoLoi,
            CreatedDate = lo.CreatedDate,
        };
    }
}
