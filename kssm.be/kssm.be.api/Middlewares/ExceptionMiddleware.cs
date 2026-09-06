using kssm.be.external.Errors.Interfaces;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace kssm.be.api.Middlewares
{
    /// <summary>
    /// Lưới chắn cuối: mọi exception lọt ra ngoài action đều thành envelope ApiResponse thay vì trang lỗi của
    /// .NET. Cần thiết vì try/catch trong action KHÔNG phủ hết - action trả bytes thô không bọc được envelope,
    /// còn lỗi model binding và lỗi ở tầng middleware thì xảy ra trước khi action chạy.
    ///
    /// Đăng ký SÁT phía trong nhất có thể: exception lan từ trong ra nên middleware nào gần action hơn sẽ bắt
    /// trước, nhờ đó nó chặn được cả trang DeveloperExceptionPage mà môi trường Development tự gắn.
    /// </summary>
    public class ExceptionMiddleware
    {
     
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private readonly RequestDelegate _next;
        private readonly IErrorResponseResolver _errorResponseResolver;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(
            RequestDelegate next,
            IErrorResponseResolver errorResponseResolver,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _errorResponseResolver = errorResponseResolver;
            _logger = logger;
        }

        /// <summary>Chạy request và quy mọi exception chưa ai bắt về envelope lỗi.</summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var response = _errorResponseResolver.Resolve(ex);

                _logger.LogError(
                    ex,
                    $"{ex.GetType()}: Path = {context.Request.Path}, ErrorCode = {response.Code}, Message = {response.Message}");

   
                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json; charset=utf-8";

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
            }
        }
    }
}
