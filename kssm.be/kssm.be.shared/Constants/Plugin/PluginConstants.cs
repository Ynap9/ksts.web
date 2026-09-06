namespace kssm.be.shared.Constants.Plugin
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

        /// <summary>Khớp đúng từng ký tự với CaiDatConstants.TenExe của plugin; lệch là API báo thiếu bộ cài.</summary>
        public const string SetupFileName = "Ký số plugin.exe";

        public const string SetupContentType = "application/octet-stream";

        /// <summary>Tên section trong appsettings.json giữ whitelist phiên bản plugin.</summary>
        public const string ConfigSection = "Plugin";

        /// <summary>
        /// Whitelist dùng khi cấu hình để trống. Ghim ở đây để một bản triển khai thiếu section Plugin vẫn
        /// chạy được, thay vì đánh trượt MỌI plugin và chặn cả hệ thống ký.
        /// </summary>
        public static readonly string[] PhienBanPhuHopMacDinh = ["1.0.0"];
    }
}
