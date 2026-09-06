using ksts.plugin.applications.Plugin.Dtos;
using ksts.plugin.applications.Plugin.Interfaces;
using ksts.plugin.shared.Constants;

namespace ksts.plugin.applications.Plugin.Implements
{
    public class PluginService : IPluginService
    {
        public ViewTrangThaiDto GetTrangThai()
        {
            return new ViewTrangThaiDto
            {
                Ten = PluginConstants.Ten,
                PhienBan = PluginConstants.PhienBan,
                SanSang = true,
            };
        }
    }
}
