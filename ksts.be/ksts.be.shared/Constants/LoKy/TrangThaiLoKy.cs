namespace ksts.be.shared.Constants.LoKy
{
    /// <summary>
    /// Trạng thái một lô ký. Đánh số TƯỜNG MINH vì giá trị đi thẳng ra JSON cho FE và đã nằm trong DB —
    /// chèn thêm phần tử vào giữa là đổi nghĩa dữ liệu cũ.
    /// </summary>
    public enum TrangThaiLoKy
    {
        /// <summary>Lô vừa mở, đang nhận file, chưa ký gì.</summary>
        MoiTao = 0,

        /// <summary>Đang ký.</summary>
        DangKy = 1,

        /// <summary>Đã chạy hết lô. Vẫn có thể còn file lỗi.</summary>
        Xong = 2,

        /// <summary>Người dùng dừng giữa chừng. File đã ký xong vẫn giữ nguyên và vẫn hợp lệ.</summary>
        Huy = 3,

        /// <summary>Lô dừng vì sự cố chung (rút token, mất kết nối), không phải lỗi của một file cụ thể.</summary>
        Loi = 4,
    }
}
