namespace ksts.be.shared.Settings
{
    /// <summary>
    /// Whitelist phiên bản plugin mà backend này chấp nhận. Để ở cấu hình chứ không ghim trong mã để dev
    /// duyệt một bản plugin mới bằng cách sửa appsettings.json, không phải build lại backend - đúng cách
    /// bộ cài đã làm (chép exe mới vào Plugins/ là xong, không cần khởi động lại).
    /// </summary>
    public class PluginSettings
    {
        public List<string> PhienBanPhuHop { get; set; } = [];
    }
}
