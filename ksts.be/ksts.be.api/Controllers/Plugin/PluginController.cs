using ksts.be.api.Controllers.Base;
using ksts.be.applications.Plugin.Interfaces;
using ksts.be.shared.Constants.Plugin;
using ksts.be.shared.Requests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ksts.be.api.Controllers.Plugin
{
    /// <summary>
    /// Phát bộ cài plugin ký số cho máy người dùng. FE dò plugin ở 127.0.0.1 trước; dò không thấy thì mở
    /// popup và tải bộ cài qua đây.
    /// </summary>
    [Route("api/core/plugin")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PluginController : BaseController
    {
        private readonly IPluginService _pluginService;

        public PluginController(
            IPluginService pluginService,
            ILogger<PluginController> logger) : base(logger)
        {
            _pluginService = pluginService;
        }

        /// <summary>Thông tin bộ cài plugin đi kèm bản build.</summary>
        [HttpGet("bo-cai")]
        public ApiResponse GetBoCai()
        {
            try
            {
                var result = _pluginService.GetBoCai();
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Nội dung bộ cài để trình duyệt tải về. KHÔNG bọc ApiResponse - đầu ra là file nén, bọc JSON vào
        /// thì tải về không giải nén được.
        /// </summary>
        [HttpGet("bo-cai/noi-dung")]
        public IActionResult GetBoCaiContent()
        {
            var stream = _pluginService.OpenBoCai();
            return File(stream, PluginConstants.SetupContentType, PluginConstants.SetupFileName);
        }
    }
}
