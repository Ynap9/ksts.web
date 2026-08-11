using ksts.be.external.Gotenberg.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using ksts.be.shared.Settings;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace ksts.be.external.Gotenberg.Implements
{
    /// <summary>
    /// Client của Gotenberg, route Chromium. Dùng IHttpClientFactory để cả lô in dùng chung pool kết nối:
    /// dựng HttpClient mới cho mỗi giấy báo sẽ cạn cổng khi in tới vài nghìn bản.
    /// </summary>
    public class GotenbergConverter : IGotenbergConverter
    {
        private const string ConvertPath = "forms/chromium/convert/html";
        private const string IndexFileName = "index.html";
        private const string ReadyExpression = "window.gbttSanSang === true";
        private const string PaperWidthInch = "8.27";
        private const string PaperHeightInch = "11.7";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ConvertFileSettings _settings;

        public GotenbergConverter(IHttpClientFactory httpClientFactory, IOptions<ConvertFileSettings> settings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
        }

        /// <inheritdoc/>
        public async Task<byte[]> HtmlToPdfAsync(string html, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            {
                throw new UserFriendlyException(ErrorCodes.ConvertFileNotConfigured,
                    "Chưa cấu hình địa chỉ dịch vụ chuyển đổi file.");
            }

            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(html));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            form.Add(file, "files", IndexFileName);

            form.Add(new StringContent(PaperWidthInch), "paperWidth");
            form.Add(new StringContent(PaperHeightInch), "paperHeight");
            form.Add(new StringContent("0"), "marginTop");
            form.Add(new StringContent("0"), "marginBottom");
            form.Add(new StringContent("0"), "marginLeft");
            form.Add(new StringContent("0"), "marginRight");
            form.Add(new StringContent("true"), "printBackground");
            form.Add(new StringContent(ReadyExpression), "waitForExpression");

            var client = _httpClientFactory.CreateClient(nameof(GotenbergConverter));
            client.BaseAddress = new Uri(_settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(ConvertPath, form, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new UserFriendlyException(ErrorCodes.ConvertFileFailed,
                    "Không gọi được dịch vụ chuyển đổi file.");
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new UserFriendlyException(ErrorCodes.ConvertFileFailed,
                        $"Dịch vụ chuyển đổi file trả về lỗi {(int)response.StatusCode}.");
                }

                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }
        }
    }
}
