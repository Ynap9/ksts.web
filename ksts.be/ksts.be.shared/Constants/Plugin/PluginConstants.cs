namespace ksts.be.shared.Constants.Plugin
{
    /// <summary>
    /// Hằng số cho bộ cài plugin ký số ở máy người dùng. Bộ cài là MỘT file exe tự cài: chạy nó là cài luôn
    /// middleware bit4id đã nhúng sẵn bên trong, chép plugin vào máy rồi chạy nền. Không giải nén, không có
    /// file phụ nào để chạy nhầm.
    /// </summary>
    public static class PluginConstants
    {
        /// <summary>
        /// Đường dẫn bộ cài đi kèm bản build. Là asset CHỈ ĐỌC nằm cạnh app nên resolve từ
        /// AppContext.BaseDirectory.
        /// </summary>
        public static string GetSetupPath() =>
            Path.Combine(AppContext.BaseDirectory, "Plugins", SetupFileName);

        public const string SetupFileName = "KstsPlugin.exe";

        public const string SetupContentType = "application/octet-stream";
    }
}
