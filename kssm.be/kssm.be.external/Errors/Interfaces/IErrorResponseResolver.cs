using kssm.be.shared.Requests;

namespace kssm.be.external.Errors.Interfaces
{
    /// <summary>
    /// Dịch một Exception thành envelope ApiResponse mang đúng mã lỗi và câu tiếng Việt cho người dùng.
    ///
    /// Tách thành service dùng chung vì có HAI chỗ phải dịch giống hệt nhau: controller bắt lỗi của chính
    /// action nó, và middleware bắt mọi thứ lọt ra ngoài action (model binding, action trả bytes thô, lỗi ở
    /// tầng middleware). Hai chỗ dịch khác nhau thì cùng một sự cố lại ra hai mã lỗi khác nhau.
    /// </summary>
    public interface IErrorResponseResolver
    {
        /// <summary>
        /// Envelope tương ứng với <paramref name="ex"/>. Luôn có Status = Error kèm Code và Message; không bao
        /// giờ trả về null và không ném lại.
        /// </summary>
        ApiResponse Resolve(Exception ex);
    }
}
