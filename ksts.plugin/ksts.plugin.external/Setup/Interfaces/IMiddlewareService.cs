namespace ksts.plugin.external.Setup.Interfaces
{
    /// <summary>
    /// Middleware đọc USB token (bit4id). Đây là thứ bắc cầu chứng thư trên token vào Windows certificate
    /// store; thiếu nó thì plugin chạy bình thường nhưng không bao giờ thấy chứng thư của người dùng.
    /// </summary>
    public interface IMiddlewareService
    {
        /// <summary>
        /// Máy đã có middleware chưa. Hỏi thẳng danh sách provider mật mã đã đăng ký với Windows chứ không dò
        /// tên trong Apps &amp; Features: cái quyết định token có hiện trong certificate store là provider có
        /// được đăng ký hay không, còn mục trong Apps &amp; Features chỉ nói ai đó từng chạy bộ cài.
        /// </summary>
        bool DaCoTrenMay();

        /// <summary>Bản exe này có nhúng kèm bộ cài middleware hay không.</summary>
        bool CoBanNhungKem();

        /// <summary>
        /// Bung bộ cài đã nhúng ra file tạm rồi chạy ở chế độ ngầm. Cần quyền quản trị vì middleware đăng ký
        /// provider mật mã cho cả máy.
        /// </summary>
        void CaiNgam();
    }
}
