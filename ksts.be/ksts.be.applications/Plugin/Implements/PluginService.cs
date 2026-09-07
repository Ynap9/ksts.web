using ksts.be.applications.Plugin.Dtos;
using ksts.be.applications.Plugin.Interfaces;
using ksts.be.shared.Constants.Plugin;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using ksts.be.shared.Settings;
using Microsoft.Extensions.Options;

namespace ksts.be.applications.Plugin.Implements
{
    public class PluginService : IPluginService
    {
        private readonly IOptionsMonitor<PluginSettings> _pluginSettings;

        public PluginService(IOptionsMonitor<PluginSettings> pluginSettings)
        {
            _pluginSettings = pluginSettings;
        }

        public ViewBoCaiPluginDto GetBoCai()
        {
            var path = PluginConstants.GetSetupPath();
            return new ViewBoCaiPluginDto
            {
                FileName = PluginConstants.GetSetupDownloadName(),
                Exists = File.Exists(path),
            };
        }

        public Stream OpenBoCai()
        {
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
