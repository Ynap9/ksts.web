using ksts.be.external.Pdf.Dtos;

namespace ksts.be.external.Pdf.Interfaces
{
    /// <summary>
    /// Đọc bản sửa đổi cuối của một file PDF: đi ngược chuỗi xref từ startxref cuối, dựng bảng vị trí object,
    /// lấy trailer (/Root, /Size, /ID) và chặn những file không ký nối bản được.
    ///
    /// Phải tự đọc chứ không dùng thư viện có sẵn vì chữ ký PDF phủ OFFSET BYTE TUYỆT ĐỐI: ký thêm bắt buộc
    /// phải nối bản, mà muốn nối bản thì phải biết chính xác object nào nằm ở đâu trong bản cũ.
    /// </summary>
    public interface IPdfRevisionReader
    {
        /// <summary>
        /// Đọc file và kiểm hai chốt fail-closed: file mã hoá (/Encrypt) và chữ ký chứng thực cấm mọi thay đổi
        /// (/DocMDP /P 1). Tầng trên bắt lỗi để đánh trượt RIÊNG file đó, cả lô vẫn chạy tiếp.
        /// </summary>
        PdfRevisionDto Load(byte[] bytes);

        /// <summary>Đọc BẢNG xref cổ điển tại offset, nạp vị trí object vào bản đọc; trả nguyên văn trailer.</summary>
        string ReadClassicTable(PdfRevisionDto revision, long offset);

        /// <summary>Đọc LUỒNG xref (/Type/XRef) tại offset; dict của nó chính là trailer.</summary>
        string ReadXrefStream(PdfRevisionDto revision, long offset);

        /// <summary>
        /// Lấy nguyên văn thân object theo số hiệu, giải nén nếu nó nằm trong object stream.
        /// Dùng để ĐỌC Catalog và trang trước khi ghi đè.
        /// </summary>
        string? GetObjectBody(PdfRevisionDto revision, int number);

        /// <summary>
        /// Lấy byte thân stream Y NGUYÊN như trong file (chưa giải nén). Chép nguyên byte đã nén thì khỏi nén
        /// lại và bản chép chắc chắn giống hệt bản gốc.
        /// </summary>
        byte[]? GetRawStreamBytes(PdfRevisionDto revision, int number);

        /// <summary>Đọc dữ liệu thân stream, giải FlateDecode và gỡ PNG predictor. Filter khác thì trả null.</summary>
        byte[]? ReadStreamData(PdfRevisionDto revision, int afterDict, string dict);

        /// <summary>Gỡ PNG predictor: mỗi dòng có một byte kiểu lọc ở đầu, giá trị lưu là HIỆU so với ô trước.</summary>
        byte[] RemovePngPredictor(byte[] raw, int columns);

        /// <summary>
        /// Cắt nguyên văn một dict "&lt;&lt; ... &gt;&gt;" bắt đầu tại <paramref name="start"/>, đếm lồng nhau và bỏ qua
        /// dấu ngoặc nằm trong chuỗi literal — nếu không, một chuỗi chứa "&gt;&gt;" sẽ cắt cụt dict.
        /// </summary>
        string ExtractDictionary(string text, int start);
    }
}
