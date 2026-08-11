namespace ksts.plugin.shared.Constants
{
    /// <summary>
    /// Hằng số nhận diện plugin. Cổng và tên đi vào cả trình cài đặt lẫn FE nên phải cố định ở một chỗ.
    /// </summary>
    public static class PluginConstants
    {
        /// <summary>
        /// Cổng lắng nghe trên 127.0.0.1. Chọn cổng cao, ít đụng phần mềm phổ biến; FE dò đúng cổng này để
        /// biết máy đã cài plugin hay chưa.
        /// </summary>
        public const int Port = 17739;

        public const string Ten = "KSTS Plugin ký số";

        public const string PhienBan = "1.0.0";

        /// <summary>
        /// Origin của trang web được phép ĐỌC kết quả trả về. Ghim trong mã chứ không chỉ để ở
        /// appsettings.json, vì bản phát hành là MỘT file exe không kèm file cấu hình nào — quên cập nhật
        /// danh sách này thì triệu chứng là "đã cài plugin mà trang web vẫn báo chưa cài", rất tốn công dò.
        ///
        /// Đây KHÔNG phải hàng rào bảo mật: header Origin do phía gọi tự đặt, curl hay mã độc đặt tuỳ ý.
        /// Nó chỉ là điều kiện để trình duyệt cho JavaScript đọc câu trả lời.
        /// </summary>
        public static readonly string[] OriginMacDinh =
        [
            "https://ksts.yna.io.vn",
            "http://localhost:4200",
            "https://localhost:4200"
        ];
    }
}
