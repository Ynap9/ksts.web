using kssm.be.api.Controllers.Base;
using kssm.be.applications.Plugin.Dtos;
using kssm.be.applications.Plugin.Interfaces;
using kssm.be.shared.Constants.Plugin;
using kssm.be.shared.Requests;
using Microsoft.AspNetCore.Mvc;

namespace kssm.be.api.Controllers.Plugin
{
    /// <summary>
    /// Phát bộ cài plugin ký số cho máy người dùng. FE dò plugin ở 127.0.0.1 trước; dò không thấy thì mở
    /// popup và tải bộ cài qua đây.
    /// </summary>
    [Route("api/core/plugin")]
    [ApiController]
    public class PluginController : BaseController
    {
        private readonly IPluginService _pluginService;

        public PluginController(
            IPluginService pluginService,
            ILogger<PluginController> logger) : base(logger)
        {
            _pluginService = pluginService;
        }

        /// <summary>
        /// Đối chiếu phiên bản plugin ở máy người dùng với whitelist của backend. Phiên bản lạc hậu trả
        /// PhuHop = false chứ không ném lỗi - FE cần lời nhắn để mời người dùng cập nhật.
        /// </summary>
        [HttpGet("phien-ban")]
        public ApiResponse KiemTraPhienBan([FromQuery] KiemTraPhienBanDto dto)
        {
            try
            {
                var result = _pluginService.KiemTraPhienBan(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
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
        /// Nội dung bộ cài để trình duyệt tải về. KHÔNG bọc ApiResponse - đầu ra là file thực thi, bọc JSON
        /// vào thì tải về không chạy được.
        /// </summary>
        [HttpGet("bo-cai/noi-dung")]
        public IActionResult GetBoCaiContent()
        {
            var stream = _pluginService.OpenBoCai();
            return File(stream, PluginConstants.SetupContentType, PluginConstants.GetSetupDownloadName());
        }
    }
}
