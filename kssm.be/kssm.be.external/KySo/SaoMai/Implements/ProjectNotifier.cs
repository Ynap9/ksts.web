using kssm.be.external.KySo.SaoMai.Interfaces;
using kssm.be.shared.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace kssm.be.external.KySo.SaoMai.Implements
{
    public class ProjectNotifier : IProjectNotifier
    {
        private const string HeaderApiKey = "X-Api-Key";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SaoMaiSettings _settings;
        private readonly ILogger<ProjectNotifier> _logger;

        public ProjectNotifier(IHttpClientFactory httpClientFactory, IOptions<SaoMaiSettings> options,
            ILogger<ProjectNotifier> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<bool> NotifySignedAsync(string projectId, int loKyId, int completed, int failed,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.CallbackUrl))
            {
                _logger.LogWarning(
                    "Chưa khai SaoMai:CallbackUrl nên lô {LoKyId} ký xong mà dự án {ProjectId} chưa được đổi trạng thái",
                    loKyId, projectId);
                return false;
            }

            try
            {
                var client = _httpClientFactory.CreateClient(nameof(ProjectNotifier));
                client.Timeout = TimeSpan.FromSeconds(_settings.CallbackTimeoutSeconds);

                using var request = new HttpRequestMessage(HttpMethod.Post, _settings.CallbackUrl)
                {
                    Content = JsonContent.Create(new
                    {
                        projectId,
                        loKyId,
                        completed,
                        failed,
                    }),
                };

                if (!string.IsNullOrWhiteSpace(_settings.CallbackApiKey))
                {
                    request.Headers.Add(HeaderApiKey, _settings.CallbackApiKey);
                }

                using var response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                _logger.LogError("Báo lô {LoKyId} ký xong bị từ chối: HTTP {Status}", loKyId,
                    (int)response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Báo lô {LoKyId} ký xong cho dự án {ProjectId} thất bại", loKyId, projectId);
                return false;
            }
        }
    }
}
