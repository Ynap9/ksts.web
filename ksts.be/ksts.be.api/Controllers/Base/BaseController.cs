using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests;
using ksts.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;



namespace ksts.be.api.Controllers.Base
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

        [NonAction]
        public ApiResponse OkException(Exception ex)
        {
            //var mapErrorCode = HttpContext.RequestServices.GetRequiredService<IMapErrorCode>();
            //var localization = HttpContext.RequestServices.GetRequiredService<LocalizationBase>();

            var request = HttpContext.Request;
            string errStr =
                $"Path = {request.Path}, Query = {JsonSerializer.Serialize(request.Query)}";
            int errorCode;
            string message = ex.Message;
            object? data = null;

            if (ex is UserFriendlyException userFriendlyException)
            {
                errorCode = userFriendlyException.ErrorCode;
                try
                {
                    if (!string.IsNullOrWhiteSpace(userFriendlyException.MessageLocalize))
                    {
                        message = userFriendlyException.MessageLocalize;
                    }
                    else
                    {
                        message = ErrorMessages.GetMessage(errorCode);
                    }
                }
                catch (Exception exc)
                {
                    message = exc.Message;
                }
                _logger?.LogError(
                    ex,
                    $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}"
                );
            }
            else if (ex is DbUpdateException dbUpdateException)
            {
                errorCode = ErrorCodes.InternalServerError;
                if (dbUpdateException.InnerException != null)
                {
                    message = dbUpdateException.InnerException.Message;
                }
                else
                {
                    message = dbUpdateException.Message;
                }
                _logger?.LogError(
                    ex,
                    $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}"
                );
            }
            else if (ex is InvalidOperationException invalidOperationException)
            {
                errorCode = ErrorCodes.InternalServerError;
                if (invalidOperationException.InnerException != null)
                {
                    message = invalidOperationException.InnerException.Message;
                }
                else
                {
                    message = invalidOperationException.Message;
                }
                _logger?.LogError(
                    ex,
                    $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}"
                );
            }
            else
            {
                errorCode = ErrorCodes.InternalServerError;
                _logger?.LogError(
                    ex,
                    $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}"
                );
                message = "Internal server error";
            }
            _logger?.LogError(
                ex,
                $"{ex.GetType()}: {errStr}, ErrorCode = {errorCode}, Message = {message}");
            
            return new ApiResponse(StatusCodeE.Error, data, errorCode, message);
        }
    }
}
