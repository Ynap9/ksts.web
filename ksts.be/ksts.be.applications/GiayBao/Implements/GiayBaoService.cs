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
    /// Mỗi file dựng xong là ĐẨY NGAY lên kho object rồi buông khỏi bộ nhớ, đĩa máy chủ không giữ gì. Bản cũ
    /// gom cả lô vào một file nén tạm trên đĩa: năm nghìn giấy báo là gần 4 GB, đủ làm đầy ổ máy chủ, mà đầy
    /// ổ thì Gotenberg cũng hỏng theo và cả lô chết giữa chừng.
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

            return rows
                .Select(r => new ViewThiSinhDto
                {
                    HoTen = _excelSheetReader.LayGiaTri(r, GiayBaoConstants.CotBatBuoc()),
                    SoVanBan = _excelSheetReader.LayGiaTri(r, [GiayBaoConstants.ColSoVanBan])
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.HoTen))
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

            var hopLe = danhSach
                .Where(r => !string.IsNullOrWhiteSpace(
                    _excelSheetReader.LayGiaTri(r, GiayBaoConstants.CotBatBuoc())))
                .ToList();

            if (hopLe.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.ExcelNoValidRow,
                    $"Không có dòng nào điền \"{GiayBaoConstants.RequiredColumn}\".");
            }

            _zipJobStore.DonHetHan();
            var job = _zipJobStore.Tao(hopLe.Count);

            // Biết ngay từ đầu giấy báo sẽ nằm ở đâu trên kho: không còn bước đẩy lên kho riêng nữa, mỗi
            // file dựng xong là lên kho luôn.
            _zipJobStore.CapNhat(job.JobId, x => x.TienToKho = GiayBaoConstants.GetKhoKeyPrefix());

            // Không await: người dùng nhận JobId ngay rồi hỏi tiến độ, thay vì giữ một request chạy 30 phút.
            _ = ChayLoAsync(job.JobId, template, hopLe);
            return _zipJobStore.Lay(job.JobId)!;
        }

        /// <inheritdoc/>
        public async Task ChayLoAsync(string jobId, string template, List<Dictionary<string, string>> hopLe)
        {
            var dongHo = Stopwatch.StartNew();
            var banDo = GiayBaoConstants.IdTheToTenCot;
            var cancellationToken = CancellationToken.None;
            var khoaDatTen = new object();
            var daDungTen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var tranDongThoi = new SemaphoreSlim(Math.Max(1, _settings.MaxDongThoi));

                var congViec = hopLe.Select(async (row, thuTu) =>
                {
                    await tranDongThoi.WaitAsync(cancellationToken);
                    try
                    {
                        var giaTri = new Dictionary<string, string>();
                        foreach (var muc in banDo)
                        {
                            giaTri[muc.Key] = _excelSheetReader.LayGiaTri(row, muc.Value);
                        }

                        var cccd = _excelSheetReader.LayGiaTri(row, GiayBaoConstants.CotDinhDanh());
                        var htmlTheoId = new Dictionary<string, string>();
                        if (!string.IsNullOrWhiteSpace(cccd))
                        {
                            htmlTheoId[GiayBaoConstants.IdQrBox] =
                                _qrCodeSvgRenderer.RenderSvg(GiayBaoConstants.QrBaseUrl + cccd.Trim());
                        }

                        var html = _htmlDocumentFiller.Fill(template, giaTri, htmlTheoId);
                        var pdf = await _gotenbergConverter.HtmlToPdfAsync(html, cancellationToken);

                        // Đặt tên bằng ĐÚNG số định danh: nó là khoá định danh thí sinh, không dấu, không
                        // khoảng trắng, nên vừa tra cứu được ngay vừa làm object key sạch trên kho. Dòng
                        // thiếu số định danh thì lùi về số thứ tự để vẫn có tên duy nhất.
                        var cccdSach = string.Concat(cccd.Trim()
                            .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                        var ten = string.IsNullOrWhiteSpace(cccdSach) ? $"{thuTu + 1}" : cccdSach;

                        string tenFile;
                        lock (khoaDatTen)
                        {
                            tenFile = ten;
                            var lan = 1;
                            while (!daDungTen.Add(tenFile))
                            {
                                tenFile = $"{ten}-{++lan}";
                            }
                        }

                        tenFile += GiayBaoConstants.PdfExtension;
                        await _s3FileStorage.UploadBytesAsync(pdf, GiayBaoConstants.GetKhoKey(tenFile),
                            GiayBaoConstants.PdfContentType, cancellationToken);

                        _zipJobStore.CapNhat(jobId, x =>
                        {
                            x.DaXong++;
                            x.DungLuong += pdf.LongLength;
                            x.TenFileDaDay.Add(tenFile);
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Dựng giấy báo hỏng ở dòng {ThuTu}", thuTu + 1);
                        _zipJobStore.CapNhat(jobId, x =>
                        {
                            x.SoLoi++;
                            x.DongLoi.Add(new DongLoiDto { ThuTu = thuTu + 1, LyDo = ex.Message });
                        });
                    }
                    finally
                    {
                        tranDongThoi.Release();
                    }
                });

                await Task.WhenAll(congViec);

                var xong = _zipJobStore.Lay(jobId);
                _logger.LogInformation("ChayLoAsync xong: {SoFile}/{TongSo} file, {Dung} MB lên kho, {Giay}s",
                    xong?.TenFileDaDay.Count, hopLe.Count, (xong?.DungLuong ?? 0) / 1024 / 1024,
                    dongHo.Elapsed.TotalSeconds);

                _zipJobStore.CapNhat(jobId, x => x.HoanTat = true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChayLoAsync hỏng sau {Giay}s", dongHo.Elapsed.TotalSeconds);
                _zipJobStore.CapNhat(jobId, x =>
                {
                    x.LoiChung = ex.Message;
                    x.HoanTat = true;
                });
            }
        }

        /// <inheritdoc/>
        public async Task GhiNenAsync(string jobId, Stream dich, CancellationToken cancellationToken)
        {
            var dongHo = Stopwatch.StartNew();

            if (_zipJobStore.Lay(jobId) == null)
            {
                throw new UserFriendlyException(ErrorCodes.GiayBaoJobNotFound,
                    "Lô dựng giấy báo không còn tồn tại.");
            }

            List<string> danhSach = [];
            _zipJobStore.CapNhat(jobId, x => danhSach = [.. x.TenFileDaDay]);

            var daGhi = 0;
            using (var archive = new ZipArchive(dich, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Nén phải ghi tuần tự, nhưng kéo file về là việc chờ mạng: giữ sẵn vài file đã tải xong
                // trong hàng đợi để lúc ghi xong file này là có ngay file kế tiếp, không đợi trọn một vòng
                // đi-về cho từng file.
                // Tên đi kèm luôn với việc tải của chính nó: file hỏng thì bỏ qua, mà số thứ tự đếm riêng sẽ
                // lệch khỏi danh sách ngay từ file hỏng đầu tiên và mọi file sau đó vào nén sai tên.
                var hangCho = new Queue<(string Ten, Task<byte[]?> Tai)>();
                var ke = 0;

                while (ke < danhSach.Count && hangCho.Count < GiayBaoConstants.SoFileTaiTruocKhiNen)
                {
                    hangCho.Enqueue((danhSach[ke], TaiMotFileAsync(danhSach[ke], cancellationToken)));
                    ke++;
                }

                while (hangCho.Count > 0)
                {
                    var (ten, tai) = hangCho.Dequeue();
                    var pdf = await tai;

                    if (ke < danhSach.Count)
                    {
                        hangCho.Enqueue((danhSach[ke], TaiMotFileAsync(danhSach[ke], cancellationToken)));
                        ke++;
                    }

                    if (pdf == null)
                    {
                        continue;
                    }

                    var entry = archive.CreateEntry(ten, CompressionLevel.Fastest);
                    using var target = entry.Open();
                    await target.WriteAsync(pdf, cancellationToken);
                    daGhi++;
                }
            }

            _logger.LogInformation("GhiNenAsync xong: {DaGhi}/{TongSo} file, {Giay}s",
                daGhi, danhSach.Count, dongHo.Elapsed.TotalSeconds);
        }

        /// <inheritdoc/>
        public async Task<byte[]?> TaiMotFileAsync(string tenFile, CancellationToken cancellationToken)
        {
            try
            {
                return await _s3FileStorage.DownloadAsync(GiayBaoConstants.GetKhoKey(tenFile), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kéo {TenFile} từ kho về để nén hỏng", tenFile);
                return null;
            }
        }
    }
}
