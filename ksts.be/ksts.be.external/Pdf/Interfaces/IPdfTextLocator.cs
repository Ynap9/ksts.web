using ksts.be.external.Pdf.Dtos;

namespace ksts.be.external.Pdf.Interfaces
{
    /// <summary>
    /// Trích CHỮ KÈM TOẠ ĐỘ từ một trang PDF, gom thành dòng.
    ///
    /// Dùng để dò mốc đặt con dấu (chức danh và tên người ký) thay vì chốt cứng toạ độ: giấy báo sinh hàng
    /// loạt từ một mẫu nhưng tên và chức danh người ký đổi được, chốt cứng là lệch ngay lần đổi đầu tiên.
    /// </summary>
    public interface IPdfTextLocator
    {
        /// <summary>
        /// Đọc một trang, trả khổ trang (point) và danh sách dòng chữ với toạ độ theo TỈ LỆ 0..1,
        /// gốc TRÊN-TRÁI, Y hướng XUỐNG - cùng hệ quy chiếu với TemplatePosition.
        /// Trang không tồn tại thì ném UserFriendlyException(SealAnchorNotFound).
        /// </summary>
        PdfPageTextDto GetPageText(Stream pdfStream, int pageNumber);

        /// <summary>
        /// Chuẩn hoá chuỗi để so mốc: bỏ dấu tiếng Việt, viết HOA, gộp mọi khoảng trắng liên tiếp thành một.
        /// Bắt buộc phải chuẩn hoá vì PDF hay tách chữ có dấu thành nhiều glyph rời và chèn khoảng trắng lạ
        /// giữa các chữ cái.
        /// </summary>
        string Normalize(string text);
    }
}
