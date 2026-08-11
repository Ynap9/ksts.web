namespace ksts.be.external.Gotenberg.Interfaces
{
    /// <summary>
    /// Gọi Gotenberg chuyển HTML sang PDF. Gotenberg chạy Chromium nên đây là nơi duy nhất biết địa chỉ
    /// dịch vụ và các tham số khổ giấy; tầng nghiệp vụ chỉ đưa HTML và nhận về byte của file PDF.
    /// </summary>
    public interface IGotenbergConverter
    {
        /// <summary>
        /// Chuyển <paramref name="html"/> thành PDF khổ A4. Chờ biểu thức trang tự bật cờ vẽ xong rồi mới
        /// chụp, nếu không PDF ra lúc font chưa về sẽ sai bố cục. Gọi thất bại ném
        /// UserFriendlyException(ConvertFileFailed).
        /// </summary>
        Task<byte[]> HtmlToPdfAsync(string html, CancellationToken cancellationToken);
    }
}
