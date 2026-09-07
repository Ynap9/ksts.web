using kssm.be.external.Errors.Interfaces;
using kssm.be.shared.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace kssm.be.api.Controllers.Base
{
    [ApiController]
    public class BaseController : ControllerBase
    {
        private readonly ILogger<BaseController> _logger;

        public BaseController(
            ILogger<BaseController> logger
        )
        {
            _logger = logger;
        }

        /// <summary>
        /// Envelope lỗi cho một action. Lấy resolver qua RequestServices chứ không qua constructor để mọi
        /// controller con giữ nguyên chữ ký quen thuộc (service của nó + ILogger), không phải khai thêm tham
        /// số mỗi lần sinh controller mới.
        /// </summary>
        [NonAction]
        public ApiResponse OkException(Exception ex)
        {
            var request = HttpContext.Request;
            string errStr =
                $"Path = {request.Path}, Query = {JsonSerializer.Serialize(request.Query)}";

            var response = HttpContext.RequestServices
                .GetRequiredService<IErrorResponseResolver>()
                .Resolve(ex);

            _logger?.LogError(
                ex,
                $"{ex.GetType()}: {errStr}, ErrorCode = {response.Code}, Message = {response.Message}");

            return response;
        }
    }
}
