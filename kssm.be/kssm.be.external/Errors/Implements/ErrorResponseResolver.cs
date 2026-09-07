using kssm.be.external.Errors.Interfaces;
using kssm.be.shared.Requests;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;

namespace kssm.be.external.Errors.Implements
{
    public class ErrorResponseResolver : IErrorResponseResolver
    {
        public ApiResponse Resolve(Exception ex)
        {
            int errorCode;
            string message;

            if (ex is UserFriendlyException userFriendlyException)
            {
                errorCode = userFriendlyException.ErrorCode;
                // Câu do chỗ ném tự đặt được ưu tiên vì nó nói rõ ngữ cảnh ("token đã cắm chưa"); không đặt
                // thì lùi về câu chung của mã lỗi trong ErrorMessages.
                message = !string.IsNullOrWhiteSpace(userFriendlyException.MessageLocalize)
                    ? userFriendlyException.MessageLocalize
                    : ErrorMessages.GetMessage(errorCode);
            }
            else if (ex is InvalidOperationException invalidOperationException)
            {
                errorCode = ErrorCodes.InternalServerError;
                message = invalidOperationException.InnerException?.Message
                    ?? invalidOperationException.Message;
            }
            else
            {
                // Lỗi không lường trước: KHÔNG trả nội dung exception ra ngoài - stack trace và câu lỗi của
                // .NET là thông tin cho người dò, chi tiết thật đã nằm trong log.
                errorCode = ErrorCodes.InternalServerError;
                message = ErrorMessages.GetMessage(ErrorCodes.InternalServerError);
            }

            return new ApiResponse(StatusCodeE.Error, null, errorCode, message);
        }
    }
}
