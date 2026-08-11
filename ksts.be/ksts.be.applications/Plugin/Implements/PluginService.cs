using ksts.be.applications.Plugin.Dtos;
using ksts.be.applications.Plugin.Interfaces;
using ksts.be.shared.Constants.Plugin;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;

namespace ksts.be.applications.Plugin.Implements
{
    /// <inheritdoc/>
    public class PluginService : IPluginService
    {
        /// <inheritdoc/>
        public ViewBoCaiPluginDto GetBoCai()
        {
            var path = PluginConstants.GetSetupPath();
            return new ViewBoCaiPluginDto
            {
                FileName = Path.GetFileName(path),
                Exists = File.Exists(path),
            };
        }

        /// <inheritdoc/>
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
    }
}
