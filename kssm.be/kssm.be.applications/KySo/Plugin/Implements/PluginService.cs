using AutoMapper;
using kssm.be.applications.Base;
using kssm.be.applications.KySo.Plugin.Dtos;
using kssm.be.applications.KySo.Plugin.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants.Plugin;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using kssm.be.shared.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace kssm.be.applications.KySo.Plugin.Implements
{

    public class PluginService : BaseService, IPluginService
    {
        private readonly IOptionsMonitor<PluginSettings> _pluginSettings;

        public PluginService(
            KssmDbContext kstsDbContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<BaseService> logger,
            IOptionsMonitor<PluginSettings> pluginSettings) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _pluginSettings = pluginSettings;
        }


        public ViewBoCaiPluginDto GetBoCai()
        {
            _logger.LogInformation($"{nameof(GetBoCai)}");

            var path = PluginConstants.GetSetupPath();
            return new ViewBoCaiPluginDto
            {
                FileName = PluginConstants.GetSetupDownloadName(),
                Exists = File.Exists(path),
            };
        }


        public Stream OpenBoCai()
        {
            _logger.LogInformation($"{nameof(OpenBoCai)}");

            var path = PluginConstants.GetSetupPath();
            if (!File.Exists(path))
            {
                throw new UserFriendlyException(ErrorCodes.PluginSetupMissing,
                    "Bản cài thiếu bộ cài plugin ký số.");
            }

            return File.OpenRead(path);
        }

        public ViewPhienBanPluginDto KiemTraPhienBan(KiemTraPhienBanDto input)
        {
            _logger.LogInformation($"{nameof(KiemTraPhienBan)} phienBan={input.PhienBan}");

            var settings = _pluginSettings.CurrentValue;
            List<string> danhSach = settings.PhienBanPhuHop.Count > 0
                ? settings.PhienBanPhuHop
                : [.. PluginConstants.PhienBanPhuHopMacDinh];

            var phienBan = input.PhienBan?.Trim() ?? string.Empty;
            if (phienBan.Length == 0)
            {
                return new ViewPhienBanPluginDto
                {
                    PhienBan = phienBan,
                    PhuHop = false,
                    DanhSachPhuHop = danhSach,
                    LyDo = "Chưa đọc được phiên bản plugin. Cài lại bộ cài mới nhất rồi thử lại.",
                };
            }

            var phuHop = danhSach.Any(x =>
                string.Equals(x?.Trim(), phienBan, StringComparison.OrdinalIgnoreCase));

            return new ViewPhienBanPluginDto
            {
                PhienBan = phienBan,
                PhuHop = phuHop,
                DanhSachPhuHop = danhSach,
                LyDo = phuHop
                    ? null
                    : $"Plugin phiên bản {phienBan} không còn phù hợp. Tải và cài lại bộ cài mới nhất.",
            };
        }
    }
}
