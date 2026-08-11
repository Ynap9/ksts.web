namespace ksts.be.external.Qr.Interfaces
{
    /// <summary>
    /// Dựng mã QR tra cứu dạng SVG để nhồi vào ô mã QR của mẫu giấy báo trúng tuyển.
    /// Sinh ở server vì Gotenberg chụp PDF ngay khi trang vẽ xong, script dựng mã chạy trễ hơn thì mất mã.
    /// </summary>
    public interface IQrCodeSvgRenderer
    {
        /// <summary>
        /// Dựng SVG mã QR cho <paramref name="noiDung"/>. SVG không mang kích thước cố định mà nhận theo ô
        /// chứa, nên nhồi vào khung nào cũng vừa khung đó.
        /// </summary>
        string RenderSvg(string noiDung);
    }
}
