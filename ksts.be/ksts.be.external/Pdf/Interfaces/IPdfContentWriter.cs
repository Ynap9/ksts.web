using ksts.be.external.Pdf.Dtos;

namespace ksts.be.external.Pdf.Interfaces
{
    /// <summary>
    /// Ghi khối CMS đã hoàn chỉnh vào đúng chỗ /Contents mà <see cref="IPdfPreparer"/> đã chừa sẵn.
    ///
    /// Đây là NỬA SAU của luồng ký, chạy sau khi plugin ở máy người dùng đã ký và server đã đóng dấu thời gian.
    /// Chỉ ghi ĐÈ lên phần đệm '0' bên trong cặp ngoặc, không đụng một byte nào khác — đụng vào là mọi offset
    /// trong /ByteRange sai và chữ ký vừa tạo hỏng.
    /// </summary>
    public interface IPdfContentWriter
    {
        /// <summary>
        /// Trả về bản PDF hoàn chỉnh. CMS lớn hơn chỗ đã chừa thì ném <c>SignatureTooLarge</c> chứ không cắt
        /// bớt: file cắt bớt vẫn mở được nhưng chữ ký sai, mà hỏng lặng lẽ thì tệ hơn hỏng có báo.
        /// </summary>
        byte[] Write(PdfPreparedDto prepared, byte[] cms);
    }
}
