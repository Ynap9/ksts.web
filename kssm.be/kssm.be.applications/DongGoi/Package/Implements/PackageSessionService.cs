using AutoMapper;
using kssm.be.applications.Base;
using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.domain.DongGoi;
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.external.KySo.Pdf.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace kssm.be.applications.DongGoi.Package.Implements
{
    public class PackageSessionService : BaseService, IPackageSessionService
    {
        private readonly IMetadataSchemaService _metadataSchemaService;
        private readonly IDossierLayoutService _dossierLayoutService;
        private readonly IVerifyRunnerService _verifyRunnerService;
        private readonly IPackageFileStorage _packageFileStorage;
        private readonly IPdfFormatInspector _pdfFormatInspector;
        private readonly IPdfSignatureInspector _pdfSignatureInspector;

        public PackageSessionService(
            KssmDbContext kstsDbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PackageSessionService> logger,
            IMapper mapper,
            IMetadataSchemaService metadataSchemaService,
            IDossierLayoutService dossierLayoutService,
            IVerifyRunnerService verifyRunnerService,
            IPackageFileStorage packageFileStorage,
            IPdfFormatInspector pdfFormatInspector,
            IPdfSignatureInspector pdfSignatureInspector
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _metadataSchemaService = metadataSchemaService;
            _dossierLayoutService = dossierLayoutService;
            _verifyRunnerService = verifyRunnerService;
            _packageFileStorage = packageFileStorage;
            _pdfFormatInspector = pdfFormatInspector;
            _pdfSignatureInspector = pdfSignatureInspector;
        }

        public async Task<ViewPhienDto> TaoPhienAsync(TaoPhienDto input, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                $"{nameof(TaoPhienAsync)} packageType={input.PackageType}, "
                + $"objectTypes={string.Join(",", input.ObjectTypes)}");

            var chon = input.ObjectTypes.Distinct().ToList();
            var capGoi = chon.Where(MetadataObjectTypeGroups.LaCapGoi).ToList();
            var capTaiLieu = chon.Where(MetadataObjectTypeGroups.LaCapTaiLieu).ToList();

            if (capGoi.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraThieuCapGoi,
                    "Phải chọn một loại đối tượng ở cấp gói: hồ sơ, tài liệu rời lẻ hoặc yêu cầu khai thác.");
            }

            if (capGoi.Count > 1)
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraNhieuCapGoi,
                    "Một gói chỉ mô tả một đối tượng ở cấp gói, không chọn được nhiều.");
            }

            if (capTaiLieu.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraThieuCapTaiLieu,
                    "Phải chọn ít nhất một loại đối tượng ở cấp tài liệu, ví dụ văn bản.");
            }

            await _metadataSchemaService.GetAsync(input.PackageType, capGoi[0], cancellationToken);

            foreach (var loai in capTaiLieu)
            {
                await _metadataSchemaService.GetAsync(input.PackageType, loai, cancellationToken);
            }

            var phien = new PackageSession
            {
                PackageType = input.PackageType,
                ObjectType = capGoi[0],
                DocumentTypes = MetadataObjectTypeGroups.Ghi(capTaiLieu),
                Status = KiemTraConstants.StatusPending,
                CreatedDate = GetVietnamTime(),
            };

            _kstsDbContext.PackageSession.Add(phien);
            await _kstsDbContext.SaveChangesAsync(cancellationToken);

            return ToViewPhienDto(phien, 0);
        }

        public async Task<ViewPhienDto> ThemFileAsync(int sessionId, IFormFileCollection files,
            IReadOnlyList<string> duongDan, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"{nameof(ThemFileAsync)} sessionId={sessionId}, soFile={files?.Count ?? 0}");

            var phien = await LayPhienAsync(sessionId, cancellationToken);
            ChotPhienChuaChay(phien);

            if (files == null || files.Count == 0)
            {
                return ToViewPhienDto(phien, 0);
            }

            if (files.Count > KiemTraConstants.MaxFilesPerBatch)
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraVuotTranMoiDot,
                    $"Mỗi đợt chỉ nhận tối đa {KiemTraConstants.MaxFilesPerBatch} file.");
            }

            var viec = new List<ViecNhanFileDto>();
            var boQua = 0;

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var duong = i < duongDan.Count ? duongDan[i] : null;
                var relativePath = _dossierLayoutService.ChuanHoaDuongDan(duong, file.FileName);
                var extension = Path.GetExtension(relativePath).ToLowerInvariant();

                if (string.Equals(extension, KiemTraConstants.XlsLegacyExtension, StringComparison.Ordinal))
                {
                    throw new UserFriendlyException(ErrorCodes.KiemTraExcelLegacyFormat,
                        $"File \"{file.FileName}\" đang ở định dạng .xls. Hãy lưu lại thành .xlsx rồi tải lên.");
                }

                if (!string.Equals(extension, KiemTraConstants.PdfExtension, StringComparison.Ordinal)
                    && !string.Equals(extension, KiemTraConstants.XlsxExtension, StringComparison.Ordinal))
                {
                    boQua++;
                    continue;
                }

                if (string.IsNullOrEmpty(phien.RootFolderName))
                {
                    phien.RootFolderName = _dossierLayoutService.LayThuMucGoc(relativePath);
                }

                viec.Add(new ViecNhanFileDto
                {
                    File = file,
                    RelativePath = relativePath,
                    FolderName = _dossierLayoutService.LayThuMuc(relativePath),
                    Extension = extension,
                    Sequence = phien.NextSequence++,
                });
            }

            if (viec.Count == 0)
            {
                phien.ModifiedDate = GetVietnamTime();
                await _kstsDbContext.SaveChangesAsync(cancellationToken);
                return ToViewPhienDto(phien, boQua);
            }

            var hoSoTheoTen = await TaoHoSoConThieuAsync(phien, viec.Select(x => x.FolderName), cancellationToken);
            var ketQua = new List<PackageDocument>();

            foreach (var dot in viec.Chunk(KiemTraConstants.ParallelFiles))
            {
                var tacVu = dot.Select(item => XuLyMotFileAsync(phien, item, hoSoTheoTen, cancellationToken));
                foreach (var tao in await Task.WhenAll(tacVu))
                {
                    if (tao != null)
                    {
                        ketQua.Add(tao);
                    }
                }
            }

            if (ketQua.Count > 0)
            {
                _kstsDbContext.PackageDocument.AddRange(ketQua);
            }

            phien.TotalDocument += ketQua.Count;
            phien.ModifiedDate = GetVietnamTime();
            await _kstsDbContext.SaveChangesAsync(cancellationToken);

            return ToViewPhienDto(phien, boQua);
        }

        public async Task<PackageDocument?> XuLyMotFileAsync(PackageSession phien, ViecNhanFileDto viec,
            IReadOnlyDictionary<string, PackageDossier> hoSoTheoTen, CancellationToken cancellationToken)
        {
            var noiDung = await DocToanBoAsync(viec.File, cancellationToken);
            var laPdf = string.Equals(viec.Extension, KiemTraConstants.PdfExtension, StringComparison.Ordinal);
            var contentType = laPdf ? KiemTraConstants.PdfContentType : KiemTraConstants.XlsxContentType;

            var objectKey = await _packageFileStorage.SaveSourceAsync(noiDung, phien.Id, viec.Sequence,
                viec.Extension, contentType, cancellationToken);

            var hoSo = hoSoTheoTen[viec.FolderName];

            if (!laPdf)
            {
                hoSo.ExcelObjectKey = objectKey;
                hoSo.ExcelFileName = Path.GetFileName(viec.RelativePath);
                hoSo.ModifiedDate = GetVietnamTime();
                return null;
            }

            var soi = _pdfFormatInspector.Inspect(noiDung);

            return new PackageDocument
            {
                SessionId = phien.Id,
                DossierId = hoSo.Id,
                RelativePath = viec.RelativePath,
                FileName = Path.GetFileName(viec.RelativePath),
                ObjectKey = objectKey,
                SizeBytes = noiDung.LongLength,
                Status = KiemTraConstants.StatusPending,
                IsSigned = _pdfSignatureInspector.HasSignature(noiDung),
                PdfAPart = soi.PdfAPart,
                PdfAConformance = soi.PdfAConformance,
                TwoLayer = soi.HasImageLayer && soi.HasTextLayer,
                PageCount = soi.PageCount,
                CreatedDate = GetVietnamTime(),
            };
        }

        public async Task<byte[]> DocToanBoAsync(IFormFile file, CancellationToken cancellationToken)
        {
            using var nguon = file.OpenReadStream();
            using var bo = new MemoryStream();
            await nguon.CopyToAsync(bo, cancellationToken);
            return bo.ToArray();
        }

        public async Task<Dictionary<string, PackageDossier>> TaoHoSoConThieuAsync(PackageSession phien,
            IEnumerable<string> folderNames, CancellationToken cancellationToken)
        {
            var dangCo = await _kstsDbContext.PackageDossier
                .Where(x => !x.Deleted && x.SessionId == phien.Id)
                .ToListAsync(cancellationToken);

            var theoTen = new Dictionary<string, PackageDossier>(StringComparer.OrdinalIgnoreCase);
            foreach (var hoSo in dangCo)
            {
                theoTen[hoSo.FolderName] = hoSo;
            }

            foreach (var ten in folderNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (theoTen.ContainsKey(ten))
                {
                    continue;
                }

                var moi = new PackageDossier
                {
                    SessionId = phien.Id,
                    FolderName = ten,
                    Status = KiemTraConstants.StatusPending,
                    CreatedDate = GetVietnamTime(),
                };

                _kstsDbContext.PackageDossier.Add(moi);
                theoTen[ten] = moi;
            }

            await _kstsDbContext.SaveChangesAsync(cancellationToken);
            return theoTen;
        }

        public async Task<ViewPhienDto> BatDauAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"{nameof(BatDauAsync)} sessionId={sessionId}");

            var phien = await LayPhienAsync(sessionId, cancellationToken);
            ChotPhienChuaChay(phien);

            var soFile = await _kstsDbContext.PackageDocument
                .CountAsync(x => !x.Deleted && x.SessionId == sessionId, cancellationToken);

            if (soFile == 0)
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraPhienRong,
                    "Phiên chưa nhận được file PDF nào để kiểm tra.");
            }

            phien.Status = KiemTraConstants.StatusRunning;
            phien.StartedDate = GetVietnamTime();
            phien.FinishedDate = null;
            phien.FailReason = null;
            phien.ModifiedDate = GetVietnamTime();
            await _kstsDbContext.SaveChangesAsync(cancellationToken);

            _verifyRunnerService.BatDau(sessionId);
            return ToViewPhienDto(phien, 0);
        }

        public async Task<ViewTienDoKiemTraDto> TienDoAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            var phien = await LayPhienAsync(sessionId, cancellationToken);

            var vuaXong = await _kstsDbContext.PackageDocument
                .AsNoTracking()
                .Where(x => !x.Deleted && x.SessionId == sessionId
                    && x.Status != KiemTraConstants.StatusPending
                    && x.Status != KiemTraConstants.StatusFailed)
                .OrderByDescending(x => x.ModifiedDate)
                .Take(KiemTraConstants.RecentDonePerPoll)
                .ToListAsync(cancellationToken);

            var loi = await _kstsDbContext.PackageDocument
                .AsNoTracking()
                .Where(x => !x.Deleted && x.SessionId == sessionId && x.Status == KiemTraConstants.StatusFailed)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            return new ViewTienDoKiemTraDto
            {
                PhienId = phien.Id,
                TrangThai = phien.Status,
                DangChay = _verifyRunnerService.DangChay(sessionId),
                HoanTat = phien.FinishedDate != null,
                TongSo = phien.TotalDocument,
                DaXong = phien.DoneDocument,
                SoLoi = phien.ErrorDocument,
                SoCanhBao = phien.WarningDocument,
                LyDoDung = phien.FailReason,
                FilesVuaXong = vuaXong.Select(ToViewFileDto).ToList(),
                FilesLoi = loi.Select(ToViewFileDto).ToList(),
            };
        }

        public async Task<ViewKetQuaKiemTraDto> KetQuaAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            var phien = await LayPhienAsync(sessionId, cancellationToken);

            var hoSo = await _kstsDbContext.PackageDossier
                .AsNoTracking()
                .Where(x => !x.Deleted && x.SessionId == sessionId)
                .OrderBy(x => x.FolderName)
                .ToListAsync(cancellationToken);

            var files = await _kstsDbContext.PackageDocument
                .AsNoTracking()
                .Where(x => !x.Deleted && x.SessionId == sessionId)
                .OrderBy(x => x.FileName)
                .ToListAsync(cancellationToken);

            var bao = DocBaoCao(phien.MatchReport);

            return new ViewKetQuaKiemTraDto
            {
                Phien = ToViewPhienDto(phien, 0),
                BaoCaoHoSo = bao.FirstOrDefault(x => MetadataObjectTypeGroups.LaCapGoi(x.ObjectType)),
                BaoCaoTaiLieu = bao.Where(x => MetadataObjectTypeGroups.LaCapTaiLieu(x.ObjectType)).ToList(),
                HoSo = hoSo
                    .Where(x => files.Any(file => file.DossierId == x.Id))
                    .Select(x => new ViewHoSoKiemTraDto
                    {
                        Id = x.Id,
                        TenThuMuc = x.FolderName,
                        MaHoSo = x.FileCode,
                        TrangThai = x.Status,
                        SoTaiLieu = x.DocumentCount,
                        Loi = DocLoi(x.ErrorSummary),
                        Files = files.Where(file => file.DossierId == x.Id).Select(ToViewFileDto).ToList(),
                    })
                    .ToList(),
            };
        }

        public async Task HuyAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"{nameof(HuyAsync)} sessionId={sessionId}");

            var phien = await LayPhienAsync(sessionId, cancellationToken);

            await _packageFileStorage.RemoveSessionAsync(sessionId, cancellationToken);

            phien.Status = KiemTraConstants.StatusCancelled;
            phien.FinishedDate = GetVietnamTime();
            phien.ModifiedDate = GetVietnamTime();
            await _kstsDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DonPhienQuaHanAsync(int soGioGiu, CancellationToken cancellationToken = default)
        {
            var moc = GetVietnamTime().AddHours(-Math.Max(1, soGioGiu));

            var quaHan = await _kstsDbContext.PackageSession
                .Where(x => !x.Deleted && x.Status != KiemTraConstants.StatusCancelled
                    && x.CreatedDate != null && x.CreatedDate < moc)
                .ToListAsync(cancellationToken);

            foreach (var phien in quaHan)
            {
                await _packageFileStorage.RemoveSessionAsync(phien.Id, cancellationToken);
                phien.Status = KiemTraConstants.StatusCancelled;
                phien.ModifiedDate = GetVietnamTime();
            }

            if (quaHan.Count > 0)
            {
                await _kstsDbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"{nameof(DonPhienQuaHanAsync)} daDon={quaHan.Count}");
            }
        }

        public async Task<PackageSession> LayPhienAsync(int sessionId, CancellationToken cancellationToken)
        {
            var phien = await _kstsDbContext.PackageSession
                .FirstOrDefaultAsync(x => !x.Deleted && x.Id == sessionId, cancellationToken);

            if (phien == null)
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraPhienNotFound,
                    "Không tìm thấy phiên kiểm tra.");
            }

            return phien;
        }

        public void ChotPhienChuaChay(PackageSession phien)
        {
            if (string.Equals(phien.Status, KiemTraConstants.StatusRunning, StringComparison.Ordinal))
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraPhienDangChay,
                    "Phiên đang kiểm tra, chờ chạy xong rồi thao tác tiếp.");
            }

            if (string.Equals(phien.Status, KiemTraConstants.StatusCancelled, StringComparison.Ordinal))
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraPhienDaDon,
                    "Phiên đã được dọn, cần tải lại thư mục.");
            }
        }

        public List<HeaderMatchReportDto> DocBaoCao(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<HeaderMatchReportDto>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<HeaderMatchReportDto>>(json)
                    ?? new List<HeaderMatchReportDto>();
            }
            catch (JsonException)
            {
                return new List<HeaderMatchReportDto>();
            }
        }

        public List<string> DocLoi(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        public ViewPhienDto ToViewPhienDto(PackageSession phien, int soFileBoQua)
        {
            return new ViewPhienDto
            {
                Id = phien.Id,
                PackageType = phien.PackageType,
                ObjectType = phien.ObjectType,
                DocumentTypes = MetadataObjectTypeGroups.Doc(phien.DocumentTypes),
                RootFolderName = phien.RootFolderName,
                Multi = phien.Multi,
                TrangThai = phien.Status,
                TongSo = phien.TotalDocument,
                DaXong = phien.DoneDocument,
                SoLoi = phien.ErrorDocument,
                SoCanhBao = phien.WarningDocument,
                SoFileBoQua = soFileBoQua,
                CreatedDate = phien.CreatedDate,
            };
        }

        public ViewFileKiemTraDto ToViewFileDto(PackageDocument file)
        {
            return new ViewFileKiemTraDto
            {
                Id = file.Id,
                FileName = file.FileName,
                RelativePath = file.RelativePath,
                DocId = file.DocId,
                TrangThai = file.Status,
                LyDo = file.Reason,
                DaKySo = file.IsSigned,
                NguoiKy = file.SignerName,
                PdfAPart = file.PdfAPart,
                PdfAConformance = file.PdfAConformance,
                HaiLop = file.TwoLayer,
                SoTrang = file.PageCount,
            };
        }
    }
}
