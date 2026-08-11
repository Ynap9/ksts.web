namespace ksts.be.external.Tsa.Interfaces
{
    /// <summary>
    /// Xin dấu thời gian RFC 3161 của TSA Ban Cơ yếu. Dấu đóng lên GIÁ TRỊ CHỮ KÝ, nhờ đó chữ ký vẫn chứng
    /// minh được thời điểm ký kể cả sau khi chứng thư hết hạn.
    /// </summary>
    public interface ITimestampClient
    {
        /// <summary>
        /// Lấy token dấu thời gian cho <paramref name="signatureValue"/>. Hết số lần thử vẫn hỏng thì ném
        /// UserFriendlyException(TimestampFailed) — luồng ký fail-closed, không phát hành chữ ký thiếu dấu.
        /// </summary>
        Task<byte[]> RequestTokenAsync(byte[] signatureValue, CancellationToken cancellationToken);

        /// <summary>Một lượt gọi TSA, không thử lại.</summary>
        Task<byte[]> LayTokenAsync(byte[] hash, CancellationToken cancellationToken);

        /// <summary>
        /// Giờ đóng dấu (genTime) trong token — mốc UTC do TSA khai, khác signingTime là giờ người ký tự khai.
        /// Token đọc không ra thì trả null chứ không ném: file đã ký xong rồi, thiếu mỗi giá trị hiển thị.
        /// </summary>
        DateTime? DocGenTime(byte[] token);
    }
}
