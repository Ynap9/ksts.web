namespace ksts.plugin.external.Setup.Interfaces
{
    /// <summary>
    /// Thao tác hệ điều hành của luồng tự cài đặt: chép file, tự khởi động, mục gỡ cài đặt, nâng quyền.
    /// Tách riêng khỏi luồng nghiệp vụ để phần quyết định "khi nào làm gì" không dính chi tiết registry.
    /// </summary>
    public interface ITuCaiDatService
    {
        /// <summary>
        /// Tiến trình đang chạy từ thư mục cài hay từ chỗ người dùng vừa tải về. Đây là mốc phân vai giữa
        /// PLUGIN và TRÌNH CÀI ĐẶT của cùng một file exe.
        /// </summary>
        bool DangChayTuThuMucCai();

        /// <summary>
        /// Có phải bản phát hành (publish single-file) hay không. Bản build lúc phát triển chạy thẳng từ
        /// thư mục bin, không được phép tự cài lên máy lập trình viên.
        /// </summary>
        bool LaBanPhatHanh();

        bool DangCoQuyenQuanTri();

        /// <summary>Chạy lại chính file exe này với quyền quản trị, chờ xong và trả về mã thoát.</summary>
        int ChayLaiVoiQuyenQuanTri(string thamSo);

        /// <summary>
        /// Dừng bản plugin đang chạy, trừ chính tiến trình này. Không dừng thì không ghi đè được file exe.
        /// </summary>
        void DungBanDangChay();

        /// <summary>Chép chính file exe đang chạy vào thư mục cài.</summary>
        void ChepVaoThuMucCai();

        void BatTuKhoiDong();

        /// <summary>Ghi mục gỡ cài đặt để người dùng gỡ được từ Apps &amp; Features như mọi phần mềm khác.</summary>
        void GhiMucGoCaiDat();

        void ChayBanDaCai();

        void XoaTuKhoiDong();

        void XoaMucGoCaiDat();

        void XoaThuMucCai();

        /// <summary>
        /// Hẹn một tiến trình rời dọn nốt thư mục cài sau khi tiến trình này thoát. Lệnh gỡ do CHÍNH file exe
        /// trong thư mục đó chạy, mà Windows không cho xoá file đang chạy.
        /// </summary>
        void HenXoaThuMucCai();
    }
}
