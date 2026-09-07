using System.Diagnostics;

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

        /// <summary>
        /// Tên file lúc TẢI VỀ, kèm phiên bản đọc từ chính bộ cài. Tên trên đĩa phải giữ nguyên
        /// <see cref="SetupFileName"/> vì trình cài đặt của plugin dò theo tên đó, nên phiên bản chỉ được gắn
        /// ở đây. Không đọc được phiên bản thì lùi về tên trơn, tải về vẫn chạy đúng.
        /// </summary>
        public static string GetSetupDownloadName()
        {
            var path = GetSetupPath();
            if (!File.Exists(path))
            {
                return SetupFileName;
            }

            var phienBan = FileVersionInfo.GetVersionInfo(path).ProductVersion?.Split('+')[0];
            if (string.IsNullOrWhiteSpace(phienBan))
            {
                return SetupFileName;
            }

            return $"{Path.GetFileNameWithoutExtension(SetupFileName)} {phienBan}"
                + Path.GetExtension(SetupFileName);
        }

        public const string SetupContentType = "application/octet-stream";

        /// <summary>Tên section trong appsettings.json giữ whitelist phiên bản plugin.</summary>
        public const string ConfigSection = "Plugin";

        /// <summary>
        /// Whitelist dùng khi cấu hình để trống. Ghim ở đây để một bản triển khai thiếu section Plugin vẫn
        /// chạy được, thay vì đánh trượt MỌI plugin và chặn cả hệ thống ký.
        /// </summary>
        public static readonly string[] PhienBanPhuHopMacDinh = ["1.0.1"];
    }
}
