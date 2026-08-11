namespace ksts.plugin.applications.CaiDat.Interfaces
{
    /// <summary>
    /// Luồng tự cài đặt của bộ cài một-file. Cùng một KstsPlugin.exe đóng hai vai: chạy từ chỗ người dùng
    /// vừa tải về thì là TRÌNH CÀI ĐẶT, chạy từ thư mục cài thì là PLUGIN.
    ///
    /// Gộp làm một file vì bước dễ hỏng nhất của bản cũ là người dùng giải nén rồi chạy nhầm KstsPlugin.exe
    /// thay vì trình cài đặt — plugin lên nhưng middleware không được cài, và triệu chứng thì giống hệt
    /// "chưa cài gì cả".
    /// </summary>
    public interface ICaiDatService
    {
        /// <summary>
        /// Lượt chạy này là để phục vụ (plugin) hay để cài. Bản build lúc phát triển luôn tính là chạy
        /// plugin — không bao giờ được tự cài lên máy lập trình viên.
        /// </summary>
        bool LaLuotChayPlugin();

        /// <summary>Cài middleware nếu thiếu, cài plugin per-user, bật tự khởi động rồi chạy bản đã cài.</summary>
        void ChayLuotCaiDat();

        /// <summary>
        /// Chỉ cài middleware. Đây là việc của tiến trình con chạy quyền quản trị; nó KHÔNG cài plugin vì
        /// đang mang tài khoản quản trị chứ không phải tài khoản người dùng.
        /// </summary>
        void ChayLuotCaiMiddleware();

        /// <summary>
        /// Gỡ plugin. KHÔNG đụng tới middleware: đó là phần mềm dùng chung cho mọi ứng dụng chữ ký số trên
        /// máy, gỡ nó là làm hỏng cả phần mềm khác.
        /// </summary>
        void ChayLuotGoCaiDat();
    }
}
