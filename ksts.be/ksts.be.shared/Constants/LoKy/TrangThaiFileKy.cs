namespace ksts.be.shared.Constants.LoKy
{
    /// <summary>
    /// Trạng thái một file trong lô. Đánh số TƯỜNG MINH vì giá trị đi thẳng ra JSON cho FE và đã nằm trong DB.
    /// </summary>
    public enum TrangThaiFileKy
    {
        /// <summary>Chờ tới lượt. Lấy việc kế tiếp LUÔN lọc theo trạng thái này nên chạy tiếp là idempotent.</summary>
        Cho = 0,

        /// <summary>Đang ký.</summary>
        DangKy = 1,

        /// <summary>Đã ký xong, file đã ký nằm trên kho.</summary>
        Xong = 2,

        /// <summary>Ký hỏng. Lỗi một file KHÔNG làm dừng lô.</summary>
        Loi = 3,
    }
}
