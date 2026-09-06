using kssm.be.applications.Plugin.Dtos;

namespace kssm.be.applications.Plugin.Interfaces
{
    /// <summary>
    /// Bộ cài plugin ký số phát cho máy người dùng. Plugin đọc chứng thư ngay trên máy người dùng - thứ mà
    /// BE chạy trên server không làm được. Xem .claude/contracts/plugin-ky-so.contract.md.
    /// </summary>
    public interface IPluginService
    {
        /// <summary>
        /// Thông tin bộ cài đi kèm bản build. Thiếu file KHÔNG phải lỗi ở đây: trả Exists = false để FE khoá
        /// nút tải kèm lời nhắn, thay vì bắn lỗi vào mặt người dùng ngay khi mở màn hình.
        /// </summary>
        ViewBoCaiPluginDto GetBoCai();

        /// <summary>Nội dung bộ cài để FE tải về. Thiếu file lúc này mới là lỗi thật.</summary>
        Stream OpenBoCai();

        /// <summary>
        /// Đối chiếu phiên bản plugin đang chạy ở máy người dùng với whitelist của backend. Phiên bản lạc
        /// hậu KHÔNG phải lỗi ở đây: trả PhuHop = false kèm LyDo để FE mời cập nhật, vì người dùng không tự
        /// gây ra chuyện đó và vẫn cần đọc được lời nhắn.
        /// </summary>
        ViewPhienBanPluginDto KiemTraPhienBan(KiemTraPhienBanDto input);
    }
}
