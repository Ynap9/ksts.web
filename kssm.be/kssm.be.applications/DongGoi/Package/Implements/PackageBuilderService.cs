using AutoMapper;
using kssm.be.applications.Base;
using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.domain.DongGoi;
using kssm.be.external.DongGoi.Dtos;
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.external.Excel.Dtos;
using kssm.be.external.Excel.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants;
using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml.Linq;

namespace kssm.be.applications.DongGoi.Package.Implements
{
    public class PackageBuilderService : BaseService, IPackageBuilderService
    {
        private const string FieldKeyMaHoSo = "fileCode";
        private const string FieldKeyMaDinhDanh = "docId";
        private const string FieldKeyTieuDe = "title";

        private readonly IMetadataSchemaService _metadataSchemaService;
        private readonly IHeaderMatchService _headerMatchService;
        private readonly IExcelSheetReader _excelSheetReader;
        private readonly IPackageFileStorage _packageFileStorage;
        private readonly IPackageXmlBuilder _packageXmlBuilder;
        private readonly IPackageArchiveBuilder _packageArchiveBuilder;
        private readonly IPackageSchemaTemplate _packageSchemaTemplate;

        public PackageBuilderService(
            KssmDbContext kstsDbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PackageBuilderService> logger,
            IMapper mapper,
            IMetadataSchemaService metadataSchemaService,
            IHeaderMatchService headerMatchService,
            IExcelSheetReader excelSheetReader,
            IPackageFileStorage packageFileStorage,
            IPackageXmlBuilder packageXmlBuilder,
            IPackageArchiveBuilder packageArchiveBuilder,
            IPackageSchemaTemplate packageSchemaTemplate
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _metadataSchemaService = metadataSchemaService;
            _headerMatchService = headerMatchService;
            _excelSheetReader = excelSheetReader;
            _packageFileStorage = packageFileStorage;
            _packageXmlBuilder = packageXmlBuilder;
            _packageArchiveBuilder = packageArchiveBuilder;
            _packageSchemaTemplate = packageSchemaTemplate;
        }

        public async Task<ViewDongGoiDto> DongGoiAsync(int sessionId, DongGoiDto dto,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"{nameof(DongGoiAsync)} sessionId={sessionId}");

            var phien = await LayPhienDaKiemAsync(sessionId, cancellationToken);

            var hoSoList = await _kstsDbContext.PackageDossier
                .Where(x => !x.Deleted && x.SessionId == sessionId
                    && x.Status != KiemTraConstants.StatusFailed && x.DocumentCount > 0)
                .OrderBy(x => x.FileCode)
                .ToListAsync(cancellationToken);

            if (hoSoList.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.DongGoiKhongCoHoSoDat,
                    "Không có hồ sơ nào đạt để đóng gói.");
            }

            var files = await _kstsDbContext.PackageDocument
                .Where(x => !x.Deleted && x.SessionId == sessionId
                    && x.Status != KiemTraConstants.StatusFailed)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var schemaHoSo = await _metadataSchemaService.GetAsync(phien.PackageType, phien.ObjectType,
                cancellationToken);

            var schemaTaiLieu = new List<PackageSchemaDto>();
            foreach (var loai in MetadataObjectTypeGroups.Doc(phien.DocumentTypes))
            {
                schemaTaiLieu.Add(await _metadataSchemaService.GetAsync(phien.PackageType, loai, cancellationToken));
            }

            var schemaEntries = _packageSchemaTemplate.Load();
            var boNhoExcel = new Dictionary<string, ExcelDaDocDto>(StringComparer.Ordinal);
            var ketQua = new ViewDongGoiDto { PhienId = sessionId };

            foreach (var hoSo in hoSoList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cuaHoSo = files.Where(x => x.DossierId == hoSo.Id).ToList();

                if (cuaHoSo.Count == 0 || string.IsNullOrWhiteSpace(hoSo.FileCode)
                    || string.IsNullOrWhiteSpace(hoSo.ExcelObjectKey))
                {
                    ketQua.BoQua.Add(hoSo.FileCode ?? hoSo.FolderName);
                    continue;
                }

                var capDto = await LaySheetAsync(boNhoExcel, hoSo.ExcelObjectKey, schemaHoSo, schemaTaiLieu,
                    cancellationToken);

                if (capDto.TaiLieu.Count == 0)
                {
                    ketQua.BoQua.Add(hoSo.FileCode);
                    continue;
                }

                var goi = await DungMotGoiAsync(phien, dto, hoSo, cuaHoSo, capDto, schemaHoSo, schemaTaiLieu,
                    schemaEntries, cancellationToken);

                ketQua.Goi.Add(goi);
                ketQua.SoTaiLieu += goi.SoTaiLieu;
            }

            ketQua.SoGoi = ketQua.Goi.Count;

            await _kstsDbContext.SaveChangesAsync(cancellationToken);

            return ketQua;
        }

        public async Task<ViewDongGoiDto> DanhSachGoiAsync(int sessionId,
            CancellationToken cancellationToken = default)
        {
            var hoSoList = await _kstsDbContext.PackageDossier
                .AsNoTracking()
                .Where(x => !x.Deleted && x.SessionId == sessionId && x.PackageObjectKey != null)
                .OrderBy(x => x.FileCode)
                .ToListAsync(cancellationToken);

            return new ViewDongGoiDto
            {
                PhienId = sessionId,
                SoGoi = hoSoList.Count,
                SoTaiLieu = hoSoList.Sum(x => x.DocumentCount),
                Goi = hoSoList.Select(x => new ViewGoiDto
                {
                    HoSoId = x.Id,
                    MaHoSo = x.FileCode ?? x.FolderName,
                    Objid = x.PackageObjid ?? string.Empty,
                    TenFile = Path.GetFileName(x.PackageObjectKey!),
                    SoTaiLieu = x.DocumentCount,
                    DungLuong = x.PackageSize,
                    Url = _packageFileStorage.BuildPublicUrl(x.PackageObjectKey!),
                }).ToList(),
            };
        }

        public async Task<TaiGoiDto> TaiGoiAsync(int sessionId, int hoSoId,
            CancellationToken cancellationToken = default)
        {
            var hoSo = await _kstsDbContext.PackageDossier
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.Deleted && x.SessionId == sessionId && x.Id == hoSoId
                    && x.PackageObjectKey != null, cancellationToken);

            if (hoSo == null)
            {
                throw new UserFriendlyException(ErrorCodes.DongGoiGoiNotFound,
                    $"Không tìm thấy gói đã đóng của hồ sơ {hoSoId}.");
            }

            return new TaiGoiDto
            {
                NoiDung = await _packageFileStorage.DownloadAsync(hoSo.PackageObjectKey!, cancellationToken),
                TenFile = Path.GetFileName(hoSo.PackageObjectKey!),
            };
        }

        public async Task<PackageSession> LayPhienDaKiemAsync(int sessionId, CancellationToken cancellationToken)
        {
            var phien = await _kstsDbContext.PackageSession
                .FirstOrDefaultAsync(x => !x.Deleted && x.Id == sessionId, cancellationToken);

            if (phien == null)
            {
                throw new UserFriendlyException(ErrorCodes.KiemTraPhienNotFound,
                    $"Không tìm thấy phiên kiểm tra {sessionId}.");
            }

            var daKiem = string.Equals(phien.Status, KiemTraConstants.StatusPassed, StringComparison.Ordinal)
                || string.Equals(phien.Status, KiemTraConstants.StatusWarning, StringComparison.Ordinal)
                || string.Equals(phien.Status, KiemTraConstants.StatusFailed, StringComparison.Ordinal);

            if (!daKiem)
            {
                throw new UserFriendlyException(ErrorCodes.DongGoiPhienChuaKiem,
                    "Phiên chưa kiểm tra xong nên chưa đóng gói được.");
            }

            return phien;
        }

        public async Task<ViewGoiDto> DungMotGoiAsync(PackageSession phien, DongGoiDto dto, PackageDossier hoSo,
            List<PackageDocument> cuaHoSo, ExcelDaDocDto capDto, PackageSchemaDto schemaHoSo,
            IReadOnlyList<PackageSchemaDto> schemaTaiLieu, IReadOnlyList<PackEntryDto> schemaEntries,
            CancellationToken cancellationToken)
        {
            var objid = _packageXmlBuilder.NewUuid();
            var created = _packageXmlBuilder.FormatDateTime(GioVietNam());
            var metsType = phien.ObjectType == MetadataObjectType.Dossier
                ? SipPackConstants.MetsTypeHoSo
                : SipPackConstants.MetsTypeTaiLieu;

            var entries = new List<PackEntryDto>(schemaEntries);

            var dongHoSo = TimDongTheoMa(capDto.SheetHoSo, capDto.HeaderMaHoSo, hoSo.FileCode!);
            var eadBytes = _packageXmlBuilder.Serialize(new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                DungSimpleDc(schemaHoSo, capDto.BaoCaoHoSo, dongHoSo)));

            entries.Add(new PackEntryDto
            {
                DuongDan = $"{SipPackConstants.MetadataDir}/{SipPackConstants.DescriptiveDir}"
                    + $"/{SipPackConstants.EadFileName}",
                NoiDung = eadBytes,
            });

            var docs = new List<PackDocDto>();

            foreach (var file in cuaHoSo)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var docId = file.DocId ?? Path.GetFileNameWithoutExtension(file.FileName);
                var noiDung = await _packageFileStorage.DownloadAsync(file.ObjectKey, cancellationToken);
                var tenData = $"{docId}{Path.GetExtension(file.FileName)}";

                var sheet = TimSheetTaiLieu(capDto, docId);
                var schema = schemaTaiLieu.First(x => x.ObjectType == sheet.Sheet.ObjectType);
                var metaBytes = _packageXmlBuilder.Serialize(new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    DungSimpleDc(schema, sheet.Sheet.BaoCao, sheet.Dong)));

                var tenMeta = $"{TienToTheoLoai(sheet.Sheet.ObjectType)}{docId}.xml";

                entries.Add(new PackEntryDto
                {
                    DuongDan = $"{SipPackConstants.RepresentationsDir}/{SipPackConstants.Rep1Dir}"
                        + $"/{SipPackConstants.DataDir}/{tenData}",
                    NoiDung = noiDung,
                });

                entries.Add(new PackEntryDto
                {
                    DuongDan = $"{SipPackConstants.RepresentationsDir}/{SipPackConstants.Rep1Dir}"
                        + $"/{SipPackConstants.MetadataDir}/{SipPackConstants.DescriptiveDir}/{tenMeta}",
                    NoiDung = metaBytes,
                });

                docs.Add(new PackDocDto
                {
                    Data = DungPackFile(tenData, noiDung, _packageXmlBuilder.NewFileId(),
                        _packageXmlBuilder.GetMimeType(Path.GetExtension(tenData))),
                    Meta = DungPackFile(tenMeta, metaBytes, _packageXmlBuilder.NewFileId(),
                        SipPackConstants.MimeTypeXml),
                    DmdSecId = _packageXmlBuilder.NewUuid(),
                    MdRefId = _packageXmlBuilder.NewUuid(),
                });
            }

            var dataFileGrpId = _packageXmlBuilder.NewUuid();
            var repMetsBytes = _packageXmlBuilder.Serialize(_packageXmlBuilder.BuildRepMets(new RepMetsInputDto
            {
                MetsType = metsType,
                Created = created,
                NguoiTao = dto.NguoiTao,
                MaPhong = dto.MaPhong,
                DataFileGrpId = dataFileGrpId,
                Docs = docs,
            }));

            entries.Add(new PackEntryDto
            {
                DuongDan = $"{SipPackConstants.RepresentationsDir}/{SipPackConstants.Rep1Dir}"
                    + $"/{SipPackConstants.MetsFileName}",
                NoiDung = repMetsBytes,
            });

            var rootMetsBytes = _packageXmlBuilder.Serialize(_packageXmlBuilder.BuildRootMets(new RootMetsInputDto
            {
                Objid = objid,
                Label = LayGiaTriTheoKhoa(schemaHoSo, capDto.BaoCaoHoSo, dongHoSo, FieldKeyTieuDe),
                MetsType = metsType,
                FileCode = hoSo.FileCode!,
                MaPhong = dto.MaPhong,
                Created = created,
                EadDmdSecId = _packageXmlBuilder.NewUuid(),
                EadMdRefId = _packageXmlBuilder.NewUuid(),
                EadSize = eadBytes.LongLength,
                EadChecksum = _packageArchiveBuilder.ComputeSha256(eadBytes),
                SchemasFileGrpId = _packageXmlBuilder.NewUuid(),
                Schemas = schemaEntries.Select(x => DungPackFile(Path.GetFileName(x.DuongDan), x.NoiDung,
                    _packageXmlBuilder.NewFileId(), SipPackConstants.MimeTypeOctetStream)).ToList(),
                RepFileGrpId = _packageXmlBuilder.NewUuid(),
                RepMets = DungPackFile(SipPackConstants.MetsFileName, repMetsBytes,
                    _packageXmlBuilder.NewFileId(), SipPackConstants.MimeTypeXml),
            }));

            entries.Add(new PackEntryDto { DuongDan = SipPackConstants.MetsFileName, NoiDung = rootMetsBytes });

            var zip = _packageArchiveBuilder.Build(objid, entries);
            var tenZip = $"{Guid.NewGuid()}{SipPackConstants.ZipExtension}";
            var objectKey = await _packageFileStorage.SavePackageAsync(zip, phien.Id, tenZip, cancellationToken);

            hoSo.PackageObjid = objid;
            hoSo.PackageObjectKey = objectKey;
            hoSo.PackageSize = zip.LongLength;
            hoSo.ModifiedDate = DateTimeConstants.VietnamNow;

            return new ViewGoiDto
            {
                HoSoId = hoSo.Id,
                MaHoSo = hoSo.FileCode!,
                Objid = objid,
                TenFile = tenZip,
                SoTaiLieu = docs.Count,
                DungLuong = zip.LongLength,
                Url = _packageFileStorage.BuildPublicUrl(objectKey),
            };
        }

        public PackFileDto DungPackFile(string tenFile, byte[] noiDung, string fileId, string mimeType)
        {
            return new PackFileDto
            {
                FileName = tenFile,
                FileId = fileId,
                MimeType = mimeType,
                Size = noiDung.LongLength,
                Checksum = _packageArchiveBuilder.ComputeSha256(noiDung),
            };
        }

        public XElement DungSimpleDc(PackageSchemaDto schema, HeaderMatchReportDto? baoCao,
            Dictionary<string, string>? dong)
        {
            var truong = new List<KeyValuePair<string, string>>();

            foreach (var field in schema.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.EadElement))
                {
                    continue;
                }

                truong.Add(new KeyValuePair<string, string>(field.EadElement,
                    LayGiaTriTheoKhoa(schema, baoCao, dong, field.FieldKey)));
            }

            return _packageXmlBuilder.BuildSimpleDc(truong);
        }

        public string LayGiaTriTheoKhoa(PackageSchemaDto schema, HeaderMatchReportDto? baoCao,
            Dictionary<string, string>? dong, string fieldKey)
        {
            if (baoCao == null || dong == null)
            {
                return string.Empty;
            }

            var header = _headerMatchService.ChonHeader(baoCao, fieldKey);

            if (header == null)
            {
                return string.Empty;
            }

            var giaTri = _excelSheetReader.LayGiaTri(dong, new[] { header }).Trim();
            var field = schema.Fields.FirstOrDefault(x => string.Equals(x.FieldKey, fieldKey,
                StringComparison.Ordinal));

            return field != null && field.Codes.Count > 0 ? TachMa(giaTri) : giaTri;
        }

        public string TachMa(string giaTri)
        {
            var viTri = giaTri.IndexOf(':');
            return viTri > 0 ? giaTri[..viTri].Trim() : giaTri;
        }

        public Dictionary<string, string>? TimDongTheoMa(ExcelSheetDto? sheet, string? header, string ma)
        {
            if (sheet == null || header == null)
            {
                return null;
            }

            return sheet.Rows.FirstOrDefault(row => string.Equals(
                _excelSheetReader.LayGiaTri(row, new[] { header }).Trim().Normalize(NormalizationForm.FormC),
                ma.Trim().Normalize(NormalizationForm.FormC),
                StringComparison.OrdinalIgnoreCase));
        }

        public DongTaiLieuDto TimSheetTaiLieu(ExcelDaDocDto capDto, string docId)
        {
            foreach (var sheet in capDto.TaiLieu)
            {
                var dong = TimDongTheoMa(sheet.Sheet, sheet.HeaderDocId, docId);

                if (dong != null)
                {
                    return new DongTaiLieuDto { Sheet = sheet, Dong = dong };
                }
            }

            return new DongTaiLieuDto { Sheet = capDto.TaiLieu[0], Dong = null };
        }

        public string TienToTheoLoai(MetadataObjectType objectType)
        {
            return objectType switch
            {
                MetadataObjectType.Image => SipPackConstants.EadPicPrefix,
                MetadataObjectType.Media => SipPackConstants.EadMediaPrefix,
                _ => SipPackConstants.EadDocPrefix,
            };
        }

        public DateTimeOffset GioVietNam()
        {
            return new DateTimeOffset(DateTimeConstants.VietnamNow,
                DateTimeConstants.VietnamTimeZone.BaseUtcOffset);
        }

        public async Task<ExcelDaDocDto> LaySheetAsync(Dictionary<string, ExcelDaDocDto> boNho, string objectKey,
            PackageSchemaDto schemaHoSo, IReadOnlyList<PackageSchemaDto> schemaTaiLieu,
            CancellationToken cancellationToken)
        {
            if (boNho.TryGetValue(objectKey, out var daCo))
            {
                return daCo;
            }

            var noiDung = await _packageFileStorage.DownloadAsync(objectKey, cancellationToken);

            using var doc = new MemoryStream(noiDung);
            var sheets = _excelSheetReader.ListSheets(doc).Select(x => x.Name).ToList();

            var ketQua = new ExcelDaDocDto();
            var tenHoSo = _headerMatchService.ChonSheet(sheets, schemaHoSo.SheetName);

            if (tenHoSo != null)
            {
                using var docHoSo = new MemoryStream(noiDung);
                ketQua.SheetHoSo = _excelSheetReader.ReadSheet(docHoSo, tenHoSo, KiemTraConstants.DongTieuDe);
                ketQua.BaoCaoHoSo = _headerMatchService.Match(schemaHoSo, ketQua.SheetHoSo.Headers);
                ketQua.HeaderMaHoSo = _headerMatchService.ChonHeader(ketQua.BaoCaoHoSo, FieldKeyMaHoSo);
            }

            foreach (var schema in schemaTaiLieu)
            {
                var ten = _headerMatchService.ChonSheet(sheets, schema.SheetName);

                if (ten == null)
                {
                    continue;
                }

                using var docTaiLieu = new MemoryStream(noiDung);
                var sheet = _excelSheetReader.ReadSheet(docTaiLieu, ten, KiemTraConstants.DongTieuDe);
                var bao = _headerMatchService.Match(schema, sheet.Headers);

                ketQua.TaiLieu.Add(new SheetTaiLieuDto
                {
                    ObjectType = schema.ObjectType,
                    Sheet = sheet,
                    BaoCao = bao,
                    HeaderDocId = _headerMatchService.ChonHeader(bao, FieldKeyMaDinhDanh),
                });
            }

            boNho[objectKey] = ketQua;
            return ketQua;
        }
    }
}
