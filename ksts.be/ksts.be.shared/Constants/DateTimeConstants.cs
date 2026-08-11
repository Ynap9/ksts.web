namespace ksts.be.shared.Constants
{
    /// <summary>Mốc thời gian dùng chung. Nghiệp vụ chạy ở Việt Nam nên mọi giờ hiển thị đều là giờ VN.</summary>
    public static class DateTimeConstants
    {
        // Việt Nam cố định UTC+7 và KHÔNG có quy ước giờ mùa hè. Vẫn tra theo id múi giờ thay vì cộng thẳng 7
        // tiếng để giờ suy ra không phụ thuộc múi giờ mà máy chạy API đang đặt.
        public static readonly TimeZoneInfo VietnamTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        /// <summary>Giờ Việt Nam hiện tại. Dùng thay cho DateTime.Now — Now lấy theo múi giờ của máy.</summary>
        public static DateTime VietnamNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

        /// <summary>Quy đổi một mốc UTC (ví dụ genTime của TSA) sang giờ Việt Nam để hiển thị.</summary>
        public static DateTime ToVietnamTime(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), VietnamTimeZone);
    }
}
