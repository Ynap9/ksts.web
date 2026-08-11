namespace ksts.be.shared.Constants.Plugin
{
    /// <summary>
    /// Hằng số cho bộ cài plugin ký số ở máy người dùng. Bộ cài là file NÉN chứa trình cài đặt; chạy trình
    /// cài đặt đó là cài luôn cả middleware bit4id, nên người dùng chỉ tải đúng một file.
    /// </summary>
    public static class PluginConstants
    {
        /// <summary>
        /// Đường dẫn bộ cài đi kèm bản build. Là asset CHỈ ĐỌC nằm cạnh app nên resolve từ
        /// AppContext.BaseDirectory.
        /// </summary>
        public static string GetSetupPath() =>
            Path.Combine(AppContext.BaseDirectory, "Plugins", SetupFileName);

        public const string SetupFileName = "ksts-plugin-setup.zip";

        public const string SetupContentType = "application/zip";
    }
}
