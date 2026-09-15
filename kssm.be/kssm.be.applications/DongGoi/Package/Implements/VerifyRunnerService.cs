using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.domain.DongGoi;
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.external.Excel.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants;
using kssm.be.shared.Constants.DongGoi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace kssm.be.applications.DongGoi.Package.Implements
{
    public class VerifyRunnerService : IVerifyRunnerService
    {
        private const string FieldKeyMaHoSo = "fileCode";
        private const string FieldKeyMaDinhDanh = "docId";
        private const string FieldKeySoThuTu = "docOrdinal";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VerifyRunnerService> _logger;
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _dangChay = new();

        public VerifyRunnerService(IServiceScopeFactory scopeFactory, ILogger<VerifyRunnerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public bool DangChay(int sessionId) => _dangChay.ContainsKey(sessionId);

        public void BatDau(int sessionId)
        {
            var nguon = new CancellationTokenSource();

            if (!_dangChay.TryAdd(sessionId, nguon))
            {
                nguon.Dispose();
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await ChayAsync(sessionId, nguon.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{nameof(ChayAsync)} sessionId={sessionId}");
                    await GhiPhienLoiAsync(sessionId, ex.Message);
                }
                finally
                {
                    _dangChay.TryRemove(sessionId, out _);
                    nguon.Dispose();
                }
            });
        }

        public async Task ChayAsync(int sessionId, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();
            var schemaService = scope.ServiceProvider.GetRequiredService<IMetadataSchemaService>();
            var matchService = scope.ServiceProvider.GetRequiredService<IHeaderMatchService>();
            var layoutService = scope.ServiceProvider.GetRequiredService<IDossierLayoutService>();
            var excelReader = scope.ServiceProvider.GetRequiredService<IExcelSheetReader>();
            var storage = scope.ServiceProvider.GetRequiredService<IPackageFileStorage>();

            var phien = await db.PackageSession
                .FirstOrDefaultAsync(x => !x.Deleted && x.Id == sessionId, cancellationToken);

            if (phien == null)
            {
                return;
            }

            var hoSoList = await db.PackageDossier
                .Where(x => !x.Deleted && x.SessionId == sessionId)
                .ToListAsync(cancellationToken);

            var files = await db.PackageDocument
                .Where(x => !x.Deleted && x.SessionId == sessionId)
                .ToListAsync(cancellationToken);

            var schemaHoSo = await schemaService.GetAsync(phien.PackageType, phien.ObjectType, cancellationToken);

            var schemaTaiLieu = new List<PackageSchemaDto>();
            foreach (var loai in MetadataObjectTypeGroups.Doc(phien.DocumentTypes))
            {
                schemaTaiLieu.Add(await schemaService.GetAsync(phien.PackageType, loai, cancellationToken));
            }

            var excelTheoThuMuc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var hoSoCoExcel = hoSoList.Where(x => !string.IsNullOrWhiteSpace(x.ExcelObjectKey)).ToList();

            foreach (var hoSo in hoSoCoExcel)
            {
                excelTheoThuMuc[hoSo.FolderName] = hoSo.ExcelObjectKey!;
            }

            foreach (var hoSo in hoSoCoExcel)
            {
                excelTheoThuMuc.TryAdd(layoutService.LayThuMucCha(hoSo.FolderName), hoSo.ExcelObjectKey!);
            }

            var excelCuaFile = new Dictionary<int, string>();
            foreach (var file in files)
            {
                var khoa = layoutService.TimExcelGanNhat(
                    layoutService.LayThuMuc(file.RelativePath), excelTheoThuMuc);

                if (khoa != null)
                {
                    excelCuaFile[file.Id] = khoa;
                }
            }

            var boNhoExcel = new Dictionary<string, ExcelDaDocDto>(StringComparer.Ordinal);
            var maTheoExcel = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var khoa in excelCuaFile.Values.Distinct(StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var docDuoc = await LayExcelAsync(boNhoExcel, khoa, schemaHoSo, schemaTaiLieu,
                    excelReader, matchService, storage, cancellationToken);

                maTheoExcel[khoa] = LayMaHoSoTrongExcel(docDuoc, excelReader);
            }

            var nhomList = GomNhomTheoMaHoSo(files, excelCuaFile, boNhoExcel, maTheoExcel,
                layoutService, excelReader);

            await GanHoSoChoNhomAsync(db, sessionId, hoSoList, nhomList, cancellationToken);

            foreach (var nhom in nhomList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                KiemMotNhom(nhom, boNhoExcel, excelReader, layoutService);
            }

            phien.Multi = nhomList.Count > 1;
            phien.MatchReport = DungBaoCao(boNhoExcel.Values.FirstOrDefault(x => x.BaoCaoHoSo != null));
            phien.DoneDocument = files.Count(x => !string.Equals(x.Status, KiemTraConstants.StatusPending,
                StringComparison.Ordinal));
            phien.ErrorDocument = files.Count(x => string.Equals(x.Status, KiemTraConstants.StatusFailed,
                StringComparison.Ordinal));
            phien.WarningDocument = files.Count(x => string.Equals(x.Status, KiemTraConstants.StatusWarning,
                StringComparison.Ordinal));

            phien.Status = phien.ErrorDocument > 0 || hoSoList.Any(x =>
                    string.Equals(x.Status, KiemTraConstants.StatusFailed, StringComparison.Ordinal))
                ? KiemTraConstants.StatusFailed
                : phien.WarningDocument > 0
                    ? KiemTraConstants.StatusWarning
                    : KiemTraConstants.StatusPassed;

            phien.FinishedDate = DateTimeConstants.VietnamNow;
            phien.ModifiedDate = DateTimeConstants.VietnamNow;

            await db.SaveChangesAsync(cancellationToken);
        }

        public List<string> LayMaHoSoTrongExcel(ExcelDaDocDto docDuoc, IExcelSheetReader excelReader)
        {
            if (docDuoc.SheetHoSo == null || docDuoc.HeaderMaHoSo == null)
            {
                return new List<string>();
            }

            return docDuoc.SheetHoSo.Rows
                .Select(row => excelReader.LayGiaTri(row, new[] { docDuoc.HeaderMaHoSo }).Trim())
                .Where(ma => !string.IsNullOrWhiteSpace(ma))
                .Select(ma => ma.Normalize(NormalizationForm.FormC))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public List<NhomHoSoDto> GomNhomTheoMaHoSo(List<PackageDocument> files,
            IReadOnlyDictionary<int, string> excelCuaFile, IReadOnlyDictionary<string, ExcelDaDocDto> boNhoExcel,
            IReadOnlyDictionary<string, List<string>> maTheoExcel, IDossierLayoutService layoutService,
            IExcelSheetReader excelReader)
        {
            var theoKhoa = new Dictionary<string, NhomHoSoDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                var docId = Path.GetFileNameWithoutExtension(file.FileName).Normalize(NormalizationForm.FormC);
                file.DocId = docId;
                file.ModifiedDate = DateTimeConstants.VietnamNow;

                var thuMuc = layoutService.LayThuMuc(file.RelativePath);
                var tenThuMuc = layoutService.TenHienThi(thuMuc);

                if (!excelCuaFile.TryGetValue(file.Id, out var khoaExcel))
                {
                    ThemVaoNhom(theoKhoa, KhoaTheoThuMuc(thuMuc), null, null, tenThuMuc, file,
                        "Không tìm thấy file Excel metadata áp dụng cho thư mục này.");
                    continue;
                }

                var docDuoc = boNhoExcel[khoaExcel];

                if (docDuoc.Loi != null)
                {
                    ThemVaoNhom(theoKhoa, KhoaTheoThuMuc(thuMuc), null, khoaExcel, tenThuMuc, file, docDuoc.Loi);
                    continue;
                }

                var maHoSo = layoutService.TimMaHoSoKhopNhat(docId, maTheoExcel[khoaExcel]);

                if (maHoSo == null)
                {
                    ThemVaoNhom(theoKhoa, KhoaTheoThuMuc(thuMuc), null, khoaExcel, tenThuMuc, file,
                        $"Mã định danh \"{docId}\" không thuộc mã hồ sơ nào khai trong "
                        + $"sheet \"{docDuoc.BaoCaoHoSo?.SheetName}\".");
                    continue;
                }

                ThemVaoNhom(theoKhoa, maHoSo, maHoSo, khoaExcel, maHoSo, file, null);
            }

            return theoKhoa.Values.ToList();
        }

        public string KhoaTheoThuMuc(string thuMuc) => "?" + thuMuc;

        public void ThemVaoNhom(Dictionary<string, NhomHoSoDto> theoKhoa, string khoa, string? fileCode,
            string? excelObjectKey, string tenHienThi, PackageDocument file, string? loi)
        {
            if (!theoKhoa.TryGetValue(khoa, out var nhom))
            {
                nhom = new NhomHoSoDto
                {
                    Khoa = khoa,
                    FileCode = fileCode,
                    ExcelObjectKey = excelObjectKey,
                    TenHienThi = string.IsNullOrWhiteSpace(tenHienThi) ? khoa : tenHienThi,
                };
                theoKhoa[khoa] = nhom;
            }

            nhom.Files.Add(file);

            if (loi != null && !nhom.Loi.Contains(loi))
            {
                nhom.Loi.Add(loi);
            }
        }

        public async Task GanHoSoChoNhomAsync(KssmDbContext db, int sessionId, List<PackageDossier> hoSoList,
            List<NhomHoSoDto> nhomList, CancellationToken cancellationToken)
        {
            foreach (var nhom in nhomList)
            {
                var hoSo = new PackageDossier
                {
                    SessionId = sessionId,
                    FolderName = nhom.TenHienThi,
                    FileCode = nhom.FileCode,
                    ExcelObjectKey = nhom.ExcelObjectKey,
                    Status = KiemTraConstants.StatusPending,
                    CreatedDate = DateTimeConstants.VietnamNow,
                };

                db.PackageDossier.Add(hoSo);
                hoSoList.Add(hoSo);
                nhom.HoSo = hoSo;
            }

            await db.SaveChangesAsync(cancellationToken);

            foreach (var nhom in nhomList)
            {
                foreach (var file in nhom.Files)
                {
                    file.DossierId = nhom.HoSo!.Id;
                }
            }

            foreach (var hoSo in hoSoList.Where(x => nhomList.All(n => n.HoSo != x)))
            {
                hoSo.Status = KiemTraConstants.StatusPassed;
                hoSo.DocumentCount = 0;
                hoSo.ModifiedDate = DateTimeConstants.VietnamNow;
            }
        }

        public void KiemMotNhom(NhomHoSoDto nhom, IReadOnlyDictionary<string, ExcelDaDocDto> boNhoExcel,
            IExcelSheetReader excelReader, IDossierLayoutService layoutService)
        {
            var hoSo = nhom.HoSo!;
            hoSo.DocumentCount = nhom.Files.Count;
            hoSo.ModifiedDate = DateTimeConstants.VietnamNow;

            if (nhom.FileCode == null || nhom.ExcelObjectKey == null)
            {
                DanhTruotCaHoSo(hoSo, nhom.Files, nhom.Loi);
                return;
            }

            var docDuoc = boNhoExcel[nhom.ExcelObjectKey];
            var maTaiLieu = TapMaTaiLieu(nhom.FileCode, docDuoc, excelReader, layoutService);
            var coMaDinhDanh = docDuoc.TaiLieu.Any(x => x.CoMaDinhDanh);

            var daGap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in nhom.Files)
            {
                var ten = file.DocId ?? string.Empty;

                if (!daGap.Add(ten))
                {
                    file.Status = KiemTraConstants.StatusFailed;
                    file.Reason = "Trùng tên file trong cùng hồ sơ.";
                    continue;
                }

                if (!coMaDinhDanh)
                {
                    file.Status = KiemTraConstants.StatusFailed;
                    file.Reason = $"Sheet tài liệu thiếu cột \"{FieldKeyMaDinhDanh}\", hoặc cặp "
                        + $"\"{FieldKeyMaHoSo}\" và \"{FieldKeySoThuTu}\", để đối chiếu tên file.";
                    continue;
                }

                if (!maTaiLieu.Contains(ten))
                {
                    file.Status = KiemTraConstants.StatusFailed;
                    file.Reason = "File không có dòng metadata tương ứng (mã định danh tài liệu).";
                    continue;
                }

                var canhBao = new List<string>();

                if (!file.IsSigned)
                {
                    canhBao.Add("chưa ký số");
                }

                if (string.IsNullOrEmpty(file.PdfAPart))
                {
                    canhBao.Add("không phải PDF/A");
                }
                else if (!file.TwoLayer)
                {
                    canhBao.Add("PDF/A nhưng thiếu lớp ảnh hoặc lớp chữ");
                }

                file.Status = canhBao.Count > 0 ? KiemTraConstants.StatusWarning : KiemTraConstants.StatusPassed;
                file.Reason = canhBao.Count > 0 ? string.Join("; ", canhBao) : null;
            }

            var thieuFile = maTaiLieu.Count - nhom.Files.Count(x =>
                !string.Equals(x.Status, KiemTraConstants.StatusFailed, StringComparison.Ordinal));
            var thieuFileGhiChu = 0;

            if (thieuFile > 0)
            {
                nhom.Loi.Add($"Có {thieuFile} dòng metadata chưa có file PDF tương ứng.");
                thieuFileGhiChu = 1;
            }

            hoSo.ErrorSummary = nhom.Loi.Count > 0 ? JsonSerializer.Serialize(nhom.Loi) : null;

            var coLoiChan = nhom.Loi.Count > thieuFileGhiChu
                || nhom.Files.Any(x => string.Equals(x.Status, KiemTraConstants.StatusFailed, StringComparison.Ordinal));
            var coCanhBao = nhom.Loi.Count > 0
                || nhom.Files.Any(x => string.Equals(x.Status, KiemTraConstants.StatusWarning, StringComparison.Ordinal));

            hoSo.Status = coLoiChan
                ? KiemTraConstants.StatusFailed
                : coCanhBao
                    ? KiemTraConstants.StatusWarning
                    : KiemTraConstants.StatusPassed;
        }

        public HashSet<string> TapMaTaiLieu(string fileCode, ExcelDaDocDto docDuoc,
            IExcelSheetReader excelReader, IDossierLayoutService layoutService)
        {
            var tap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var taiLieu in docDuoc.TaiLieu)
            {
                if (!taiLieu.CoMaDinhDanh)
                {
                    continue;
                }

                foreach (var row in taiLieu.Sheet.Rows)
                {
                    var ma = GhepMaDinhDanh(taiLieu, row, excelReader);

                    if (string.IsNullOrWhiteSpace(ma) || !layoutService.LaMaCuaTaiLieu(fileCode, ma))
                    {
                        continue;
                    }

                    tap.Add(ma.Normalize(NormalizationForm.FormC));
                }
            }

            return tap;
        }

        public string GhepMaDinhDanh(SheetTaiLieuDto taiLieu, Dictionary<string, string> row,
            IExcelSheetReader excelReader)
        {
            if (taiLieu.HeaderDocId != null)
            {
                return excelReader.LayGiaTri(row, new[] { taiLieu.HeaderDocId }).Trim();
            }

            if (taiLieu.HeaderMaHoSo == null || taiLieu.HeaderSoThuTu == null)
            {
                return string.Empty;
            }

            var maHoSo = excelReader.LayGiaTri(row, new[] { taiLieu.HeaderMaHoSo }).Trim();
            var soThuTu = excelReader.LayGiaTri(row, new[] { taiLieu.HeaderSoThuTu }).Trim();

            if (string.IsNullOrWhiteSpace(maHoSo) || string.IsNullOrWhiteSpace(soThuTu))
            {
                return string.Empty;
            }

            return maHoSo + KiemTraConstants.DauNoiMaTaiLieu + soThuTu;
        }

        public async Task<ExcelDaDocDto> LayExcelAsync(Dictionary<string, ExcelDaDocDto> boNho, string objectKey,
            PackageSchemaDto schemaHoSo, IReadOnlyList<PackageSchemaDto> schemaTaiLieu, IExcelSheetReader excelReader,
            IHeaderMatchService matchService, IPackageFileStorage storage, CancellationToken cancellationToken)
        {
            if (boNho.TryGetValue(objectKey, out var daCo))
            {
                return daCo;
            }

            var ketQua = new ExcelDaDocDto();

            try
            {
                var noiDung = await storage.DownloadAsync(objectKey, cancellationToken);

                using var doc = new MemoryStream(noiDung);
                var sheets = excelReader.ListSheets(doc).Select(x => x.Name).ToList();

                var tenHoSo = matchService.ChonSheet(sheets, schemaHoSo.SheetName);
                if (tenHoSo == null)
                {
                    ketQua.Loi = $"File Excel thiếu sheet \"{schemaHoSo.SheetName}\".";
                    boNho[objectKey] = ketQua;
                    return ketQua;
                }

                using var docHoSo = new MemoryStream(noiDung);
                ketQua.SheetHoSo = excelReader.ReadSheet(docHoSo, tenHoSo, KiemTraConstants.DongTieuDe);
                ketQua.BaoCaoHoSo = matchService.Match(schemaHoSo, ketQua.SheetHoSo.Headers);
                ketQua.HeaderMaHoSo = matchService.ChonHeader(ketQua.BaoCaoHoSo, FieldKeyMaHoSo);

                foreach (var schema in schemaTaiLieu)
                {
                    var ten = matchService.ChonSheet(sheets, schema.SheetName);
                    if (ten == null)
                    {
                        ketQua.Loi = $"File Excel thiếu sheet \"{schema.SheetName}\".";
                        boNho[objectKey] = ketQua;
                        return ketQua;
                    }

                    using var docTaiLieu = new MemoryStream(noiDung);
                    var sheet = excelReader.ReadSheet(docTaiLieu, ten, KiemTraConstants.DongTieuDe);
                    var bao = matchService.Match(schema, sheet.Headers);

                    ketQua.TaiLieu.Add(new SheetTaiLieuDto
                    {
                        ObjectType = schema.ObjectType,
                        Sheet = sheet,
                        BaoCao = bao,
                        HeaderDocId = matchService.ChonHeader(bao, FieldKeyMaDinhDanh),
                        HeaderMaHoSo = matchService.ChonHeader(bao, FieldKeyMaHoSo),
                        HeaderSoThuTu = matchService.ChonHeader(bao, FieldKeySoThuTu),
                    });
                }

                var chuaDat = ketQua.TaiLieu.Where(x => !x.BaoCao.Passed).Select(x => x.BaoCao).ToList();

                if (!ketQua.BaoCaoHoSo.Passed || chuaDat.Count > 0)
                {
                    var thieu = ketQua.BaoCaoHoSo.Missing
                        .Concat(chuaDat.SelectMany(x => x.Missing))
                        .Where(x => x.Requirement == FieldRequirement.Required)
                        .Select(x => x.DisplayName);
                    ketQua.Loi = $"File Excel thiếu trường bắt buộc: {string.Join(", ", thieu)}.";
                }
            }
            catch (Exception ex)
            {
                ketQua.Loi = ex.Message;
            }

            boNho[objectKey] = ketQua;
            return ketQua;
        }

        public void DanhTruotCaHoSo(PackageDossier hoSo, List<PackageDocument> cuaHoSo, List<string> loiHoSo)
        {
            hoSo.Status = KiemTraConstants.StatusFailed;
            hoSo.ErrorSummary = JsonSerializer.Serialize(loiHoSo);
            hoSo.ModifiedDate = DateTimeConstants.VietnamNow;

            foreach (var file in cuaHoSo)
            {
                file.Status = KiemTraConstants.StatusFailed;
                file.Reason = loiHoSo.FirstOrDefault();
                file.ModifiedDate = DateTimeConstants.VietnamNow;
            }
        }

        public string? DungBaoCao(ExcelDaDocDto? nguon)
        {
            if (nguon?.BaoCaoHoSo == null)
            {
                return null;
            }

            var bao = new List<HeaderMatchReportDto> { nguon.BaoCaoHoSo };
            bao.AddRange(nguon.TaiLieu.Select(x => x.BaoCao));

            return JsonSerializer.Serialize(bao);
        }

        public async Task GhiPhienLoiAsync(int sessionId, string lyDo)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();

                var phien = await db.PackageSession.FirstOrDefaultAsync(x => x.Id == sessionId);
                if (phien == null)
                {
                    return;
                }

                phien.Status = KiemTraConstants.StatusFailed;
                phien.FailReason = lyDo;
                phien.FinishedDate = DateTimeConstants.VietnamNow;
                phien.ModifiedDate = DateTimeConstants.VietnamNow;
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(GhiPhienLoiAsync)} sessionId={sessionId}");
            }
        }
    }
}
