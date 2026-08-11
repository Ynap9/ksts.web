using AutoMapper;
using ksts.be.applications.Base;
using ksts.be.applications.GiayBao.Dtos;
using ksts.be.applications.GiayBao.Interfaces;
using ksts.be.external.Excel.Dtos;
using ksts.be.external.Excel.Interfaces;
using ksts.be.external.Gotenberg.Interfaces;
using ksts.be.external.Html.Interfaces;
using ksts.be.external.Jobs.Dtos;
using ksts.be.external.Jobs.Interfaces;
using ksts.be.external.Qr.Interfaces;
using ksts.be.external.Storage.Interfaces;
using ksts.be.infrastructure.Persistence;
using ksts.be.shared.Constants.GiayBao;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using ksts.be.shared.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO.Compression;

namespace ksts.be.applications.GiayBao.Implements
{
    /// <summary>
    /// Dựng cả lô giấy báo trúng tuyển. Chuyển HTML sang PDF là việc chờ mạng chứ không tốn CPU, nên chạy
    /// nhiều bản song song với trần đồng thời lấy từ cấu hình; thả hết một lúc sẽ làm ngộp Gotenberg.
    ///
    /// Kết quả ghi thẳng vào file nén tạm trên đĩa thay vì giữ trong bộ nhớ: năm nghìn giấy báo cỡ vài GB,
    /// gom hết vào RAM là chết tiến trình API.
    /// </summary>
    public class GiayBaoService : BaseService, IGiayBaoService
    {
        private readonly IExcelSheetReader _excelSheetReader;
        private readonly IQrCodeSvgRenderer _qrCodeSvgRenderer;
        private readonly IHtmlDocumentFiller _htmlDocumentFiller;
        private readonly IGotenbergConverter _gotenbergConverter;
        private readonly IZipJobStore _zipJobStore;
        private readonly IS3FileStorage _s3FileStorage;
        private readonly ConvertFileSettings _settings;

        public GiayBaoService(
            KstsDbContext kstsDbContext,
            ILogger<GiayBaoService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IExcelSheetReader excelSheetReader,
            IQrCodeSvgRenderer qrCodeSvgRenderer,
            IHtmlDocumentFiller htmlDocumentFiller,
            IGotenbergConverter gotenbergConverter,
            IZipJobStore zipJobStore,
            IS3FileStorage s3FileStorage,
            IOptions<ConvertFileSettings> settings)
            : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _excelSheetReader = excelSheetReader;
            _qrCodeSvgRenderer = qrCodeSvgRenderer;
            _htmlDocumentFiller = htmlDocumentFiller;
            _gotenbergConverter = gotenbergConverter;
            _zipJobStore = zipJobStore;
            _s3FileStorage = s3FileStorage;
            _settings = settings.Value;
        }

        /// <inheritdoc/>
        public List<ExcelSheetInfoDto> DanhSachSheet(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            return _excelSheetReader.ListSheets(stream);
        }

        /// <inheritdoc/>
        public List<ViewThiSinhDto> DanhSachThiSinh(IFormFile file, string? sheetName, int startRow)
        {
            using var stream = file.OpenReadStream();
            var rows = _excelSheetReader.ReadSheet(stream, sheetName, startRow).Rows;

            var khoaHoTen = _excelSheetReader.NormalizeKey(GiayBaoConstants.ColHoTen);
            var khoaSoVanBan = _excelSheetReader.NormalizeKey(GiayBaoConstants.ColSoVanBan);

            return rows
                .Where(r => r.TryGetValue(khoaHoTen, out var ten) && !string.IsNullOrWhiteSpace(ten))
                .Select(r => new ViewThiSinhDto
                {
                    HoTen = r[khoaHoTen],
                    SoVanBan = r.TryGetValue(khoaSoVanBan, out var so) ? so : string.Empty
                })
                .ToList();
        }

        /// <inheritdoc/>
        public ZipJobDto BatDauTaoZip(IFormFile file, string? sheetName, int startRow)
        {
            _logger.LogInformation("BatDauTaoZip: {FileName} sheet {Sheet} dòng {StartRow}, đồng thời {MaxDongThoi}",
                file?.FileName, sheetName, startRow, _settings.MaxDongThoi);

            var templatePath = GiayBaoConstants.GetTemplatePath();
            if (!File.Exists(templatePath))
            {
                throw new UserFriendlyException(ErrorCodes.GiayBaoTemplateMissing,
                    "Không tìm thấy mẫu giấy báo trúng tuyển đi kèm bản build.");
            }

            var template = File.ReadAllText(templatePath);

            List<Dictionary<string, string>> danhSach;
            using (var stream = file!.OpenReadStream())
            {
                danhSach = _excelSheetReader.ReadSheet(stream, sheetName, startRow).Rows;
            }

            var khoaBatBuoc = _excelSheetReader.NormalizeKey(GiayBaoConstants.RequiredColumn);
            var hopLe = danhSach
                .Where(r => r.TryGetValue(khoaBatBuoc, out var ten) && !string.IsNullOrWhiteSpace(ten))
                .ToList();

            if (hopLe.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.ExcelNoValidRow,
                    $"Không có dòng nào điền \"{GiayBaoConstants.RequiredColumn}\".");
            }

            _zipJobStore.DonHetHan();
            var job = _zipJobStore.Tao(hopLe.Count);

            // Không await: người dùng nhận JobId ngay rồi hỏi tiến độ, thay vì giữ một request chạy 30 phút.
            _ = ChayLoAsync(job.JobId, template, hopLe);
            return job;
        }

        /// <inheritdoc/>
        public async Task ChayLoAsync(string jobId, string template, List<Dictionary<string, string>> hopLe)
        {
            var dongHo = Stopwatch.StartNew();
            var banDo = GiayBaoConstants.ColumnToElementId
                .ToDictionary(x => _excelSheetReader.NormalizeKey(x.Key), x => x.Value);
            var khoaCccd = _excelSheetReader.NormalizeKey(GiayBaoConstants.ColCccd);
            var cancellationToken = CancellationToken.None;

            // Giữ lại sau khi dựng xong để người dùng tải; ZipJobStore dọn khi lô hết hạn.
            var zipPath = Path.Combine(Path.GetTempPath(), $"giay-bao-{jobId}.zip");
            var zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None,
                bufferSize: 81920);
            var daDungTen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var khoaGhi = new SemaphoreSlim(1, 1);
                    var tranDongThoi = new SemaphoreSlim(Math.Max(1, _settings.MaxDongThoi));

                    var congViec = hopLe.Select(async (row, thuTu) =>
                    {
                        await tranDongThoi.WaitAsync(cancellationToken);
                        try
                        {
                            var giaTri = new Dictionary<string, string>();
                            foreach (var muc in banDo)
                            {
                                giaTri[muc.Value] = row.TryGetValue(muc.Key, out var value) ? value : string.Empty;
                            }

                            var cccd = row.TryGetValue(khoaCccd, out var so) ? so : string.Empty;
                            var htmlTheoId = new Dictionary<string, string>();
                            if (!string.IsNullOrWhiteSpace(cccd))
                            {
                                htmlTheoId[GiayBaoConstants.IdQrBox] =
                                    _qrCodeSvgRenderer.RenderSvg(GiayBaoConstants.QrBaseUrl + cccd.Trim());
                            }

                            var html = _htmlDocumentFiller.Fill(template, giaTri, htmlTheoId);
                            var pdf = await _gotenbergConverter.HtmlToPdfAsync(html, cancellationToken);

                            // Đặt tên bằng ĐÚNG số CCCD: nó là khoá định danh thí sinh, không dấu, không
                            // khoảng trắng, nên vừa tra cứu được ngay vừa làm object key sạch khi đẩy lên
                            // kho. Dòng thiếu CCCD thì lùi về số thứ tự để vẫn có tên duy nhất.
                            var cccdSach = string.Concat(cccd.Trim()
                                .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                            var ten = string.IsNullOrWhiteSpace(cccdSach) ? $"{thuTu + 1}" : cccdSach;

                            await khoaGhi.WaitAsync(cancellationToken);
                            try
                            {
                                var tenFile = ten;
                                var lan = 1;
                                while (!daDungTen.Add(tenFile))
                                {
                                    tenFile = $"{ten}-{++lan}";
                                }

                                var entry = archive.CreateEntry(tenFile + GiayBaoConstants.PdfExtension,
                                    CompressionLevel.Fastest);
                                using var target = entry.Open();
                                await target.WriteAsync(pdf, cancellationToken);
                            }
                            finally
                            {
                                khoaGhi.Release();
                            }

                            _zipJobStore.CapNhat(jobId, x => x.DaXong++);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Dựng giấy báo hỏng ở dòng {ThuTu}", thuTu + 1);
                            _zipJobStore.CapNhat(jobId, x => x.SoLoi++);
                        }
                        finally
                        {
                            tranDongThoi.Release();
                        }
                    });

                    await Task.WhenAll(congViec);
                }

                var dungLuong = zipStream.Length;
                await zipStream.DisposeAsync();

                _logger.LogInformation("ChayLoAsync xong: {SoFile}/{TongSo} file, {Dung} MB, {Giay}s",
                    daDungTen.Count, hopLe.Count, dungLuong / 1024 / 1024, dongHo.Elapsed.TotalSeconds);

                _zipJobStore.CapNhat(jobId, x =>
                {
                    x.DuongDanZip = zipPath;
                    x.DungLuong = dungLuong;
                    x.HoanTat = true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChayLoAsync hỏng sau {SoFile}/{TongSo} file, {Giay}s",
                    daDungTen.Count, hopLe.Count, dongHo.Elapsed.TotalSeconds);
                await zipStream.DisposeAsync();
                _zipJobStore.CapNhat(jobId, x =>
                {
                    x.LoiChung = ex.Message;
                    x.HoanTat = true;
                });
            }
        }

        /// <inheritdoc/>
        public ZipJobDto BatDauDayLenKho(string jobId)
        {
            _logger.LogInformation("BatDauDayLenKho: {JobId}, đồng thời {SoLuong}",
                jobId, GiayBaoConstants.SoFileDayLenKhoSongSong);

            var job = _zipJobStore.Lay(jobId)
                ?? throw new UserFriendlyException(ErrorCodes.GiayBaoJobNotFound,
                    "Lô dựng giấy báo không còn tồn tại.");

            if (!job.HoanTat || job.DuongDanZip == null || !File.Exists(job.DuongDanZip))
            {
                throw new UserFriendlyException(ErrorCodes.GiayBaoChuaDungXong,
                    "Lô chưa dựng xong nên chưa có file để đẩy lên kho.");
            }

            // Giành quyền chạy NGAY TRONG khoá của lô: kiểm cờ rồi mới đặt ở hai bước tách rời thì bấm nút
            // hai lần thật nhanh sẽ lọt cả hai, thành hai lượt đẩy chồng nhau cùng cộng vào một bộ đếm.
            // Đặt lại bộ đếm ở đây luôn, để lần đẩy lại sau khi hỏng không cộng dồn lên số cũ.
            var giuDuoc = false;
            _zipJobStore.CapNhat(jobId, x =>
            {
                if (x.DangDayLenKho)
                {
                    return;
                }

                giuDuoc = true;
                x.DangDayLenKho = true;
                x.HoanTatDayLenKho = false;
                x.DaDayLenKho = 0;
                x.SoLoiDayLenKho = 0;
                x.LoiDayLenKho = null;
                x.TienToKho = GiayBaoConstants.GetKhoKeyPrefix();
            });

            if (!giuDuoc)
            {
                throw new UserFriendlyException(ErrorCodes.GiayBaoDangDayLenKho,
                    "Lô này đang được đẩy lên kho, chờ xong rồi hãy đẩy lại.");
            }

            // Không await: 5000 file đẩy lên kho mất hàng chục phút, giữ một request suốt ngần ấy thời gian
            // thì trình duyệt đã cắt kết nối từ lâu — giống hệt khâu dựng, FE hỏi tiến độ.
            _ = ChayDayLenKhoAsync(jobId, job.DuongDanZip);
            return _zipJobStore.Lay(jobId)!;
        }

        /// <inheritdoc/>
        public async Task ChayDayLenKhoAsync(string jobId, string zipPath)
        {
            var dongHo = Stopwatch.StartNew();

            try
            {
                List<string> tenFile;
                using (var mucLuc = ZipFile.OpenRead(zipPath))
                {
                    tenFile = mucLuc.Entries.Select(x => x.FullName).ToList();
                }

                var ke = -1;
                var luong = Enumerable.Range(0, GiayBaoConstants.SoFileDayLenKhoSongSong)
                    .Select(_ => Task.Run(async () =>
                    {
                        // Mỗi luồng mở BẢN ĐỌC RIÊNG của file nén: một ZipArchive không dùng chung được giữa
                        // nhiều luồng vì các entry chia nhau một con trỏ đọc, nhưng mở nhiều bản chỉ-đọc trên
                        // cùng một file thì được.
                        using var archive = ZipFile.OpenRead(zipPath);

                        while (true)
                        {
                            var thuTu = Interlocked.Increment(ref ke);
                            if (thuTu >= tenFile.Count)
                            {
                                break;
                            }

                            await DayMotFileAsync(jobId, archive, tenFile[thuTu]);
                        }
                    }))
                    .ToArray();

                await Task.WhenAll(luong);

                var xong = _zipJobStore.Lay(jobId);
                _logger.LogInformation("ChayDayLenKhoAsync xong: {DaDay}/{TongSo} file, lỗi {SoLoi}, {Giay}s",
                    xong?.DaDayLenKho, tenFile.Count, xong?.SoLoiDayLenKho, dongHo.Elapsed.TotalSeconds);

                _zipJobStore.CapNhat(jobId, x =>
                {
                    x.DangDayLenKho = false;
                    x.HoanTatDayLenKho = true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChayDayLenKhoAsync hỏng sau {Giay}s", dongHo.Elapsed.TotalSeconds);
                _zipJobStore.CapNhat(jobId, x =>
                {
                    x.DangDayLenKho = false;
                    x.HoanTatDayLenKho = true;
                    x.LoiDayLenKho = ex.Message;
                });
            }
        }

        /// <inheritdoc/>
        public async Task DayMotFileAsync(string jobId, ZipArchive archive, string tenFile)
        {
            try
            {
                var entry = archive.GetEntry(tenFile)
                    ?? throw new UserFriendlyException(ErrorCodes.GiayBaoJobNotFound,
                        $"Không còn thấy {tenFile} trong file nén của lô.");

                // Nạp trọn một file vào bộ nhớ vì kho object cần biết trước độ dài nội dung. Một giấy báo
                // cỡ 1 MB, tám luồng là vài MB — khác hẳn việc gom cả lô vài GB vào RAM.
                using var nguon = entry.Open();
                using var bo = new MemoryStream();
                await nguon.CopyToAsync(bo);

                await _s3FileStorage.UploadBytesAsync(bo.ToArray(),
                    GiayBaoConstants.GetKhoKey(tenFile), GiayBaoConstants.PdfContentType);

                _zipJobStore.CapNhat(jobId, x => x.DaDayLenKho++);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Đẩy {TenFile} lên kho hỏng", tenFile);
                _zipJobStore.CapNhat(jobId, x => x.SoLoiDayLenKho++);
            }
        }
    }
}
