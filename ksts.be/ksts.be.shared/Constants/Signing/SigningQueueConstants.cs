namespace ksts.be.shared.Constants.Signing
{
    /// <summary>
    /// Hằng số của hàng đợi chờ chữ ký từ máy người dùng. Ba con số này quyết định lô 5000 file chạy bao lâu,
    /// nên đổi cái nào cũng phải đo lại.
    /// </summary>
    public static class SigningQueueConstants
    {
        /// <summary>
        /// Một yêu cầu ký sống tối đa ngần này giây. Đủ dài để người dùng kịp bấm qua hộp PIN nếu middleware
        /// hỏi lại giữa chừng, nhưng phải hữu hạn: máy người dùng đóng tab thì lượt ký đó cần chết hẳn để
        /// file tính là lỗi, thay vì giữ một luồng ký treo mãi.
        /// </summary>
        public const int GiaySongCuaYeuCau = 120;

        /// <summary>
        /// Số yêu cầu trao cho máy người dùng trong MỘT đợt. Token ký tuần tự nên độ trễ đường truyền cộng
        /// thẳng vào từng file; gom theo đợt là cách duy nhất chia nhỏ khoản đó. Lấy bằng số file dựng song
        /// song vì tại một thời điểm cũng chỉ có ngần ấy file chờ ký.
        /// </summary>
        public const int SoYeuCauMoiDot = 8;

        /// <summary>
        /// Thời gian giữ lời gọi lấy việc khi chưa có việc. Ngắn hơn thì trang web hỏi lại liên tục, dài hơn
        /// thì dễ chạm giới hạn thời gian của proxy đứng trước máy chủ.
        /// </summary>
        public const int GiayChoLayViec = 25;
    }
}
