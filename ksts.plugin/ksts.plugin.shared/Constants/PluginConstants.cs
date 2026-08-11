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
    }
}
