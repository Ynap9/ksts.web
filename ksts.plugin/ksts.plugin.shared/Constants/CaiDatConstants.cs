namespace ksts.plugin.shared.Constants
{
    /// <summary>
    /// Hằng số của luồng tự cài đặt. Một file exe đóng hai vai — trình cài đặt và plugin — nên mọi mốc nhận
    /// đường (thư mục cài, khoá registry, tham số dòng lệnh) phải cố định ở đúng một chỗ.
    /// </summary>
    public static class CaiDatConstants
    {
        /// <summary>
        /// Cài per-user vào %LocalAppData%: không cần quyền quản trị, không cài driver, không dựng service
        /// SYSTEM. Chỉ riêng bước cài middleware mới phải nâng quyền.
        /// </summary>
        public static string DuongDanThuMucCai() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), TenThuMucCai);

        public const string TenThuMucCai = "KstsPlugin";

        public const string TenExe = "KstsPlugin.exe";

        /// <summary>Tự khởi động theo NGƯỜI DÙNG (HKCU) chứ không theo máy — đây là lý do cài không cần UAC.</summary>
        public const string KhoaAutostart = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public const string TenAutostart = "KstsPlugin";

        /// <summary>
        /// Mục hiện trong Apps &amp; Features. Ghi ở HKCU vì bản cài là per-user; gỡ được là bắt buộc với
        /// phần mềm chạy nền, tự khởi động và đụng tới token.
        /// </summary>
        public const string KhoaGoCaiDat = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\KstsPlugin";

        /// <summary>
        /// Tiến trình con chạy với quyền quản trị chỉ làm ĐÚNG việc cài middleware. Không để nó cài luôn
        /// plugin: nó chạy dưới tài khoản quản trị nên sẽ cài vào %LocalAppData% của tài khoản đó, sai người.
        /// </summary>
        public const string ThamSoCaiMiddleware = "--cai-middleware";

        public const string ThamSoGoCaiDat = "--go-cai-dat";

        /// <summary>Tên tài nguyên nhúng của bộ cài middleware, do ksts.plugin.api.csproj đặt lúc build.</summary>
        public const string TaiNguyenMiddlewareExe = "bit4id-setup.exe";

        public const string TaiNguyenMiddlewareMsi = "bit4id-setup.msi";

        /// <summary>
        /// Cờ chạy ngầm của bộ cài middleware. Bản bit4id đang dùng đóng bằng NSIS nên là <c>/S</c>; đổi sang
        /// bản đóng bằng InstallShield hay Inno Setup thì phải sửa đúng cờ của loại đó, sai cờ là trình cài
        /// đứng im chờ một hộp thoại không ai nhìn thấy.
        /// </summary>
        public const string CoNgamExe = "/S";

        public const string CoNgamMsi = "/qn /norestart";

        /// <summary>Mã 3010 nghĩa là cài xong nhưng cần khởi động lại máy — vẫn tính thành công.</summary>
        public const int MaLoiCanKhoiDongLai = 3010;

        /// <summary>Chờ tiến trình cũ nhả file exe trước khi ghi đè.</summary>
        public const int ChoNhaFileMs = 500;
    }
}
