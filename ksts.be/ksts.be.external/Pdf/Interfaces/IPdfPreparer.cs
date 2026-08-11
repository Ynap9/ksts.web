using ksts.be.external.Pdf.Dtos;

namespace ksts.be.external.Pdf.Interfaces
{
    /// <summary>
    /// Dựng bản ký cho một file PDF bằng cách NỐI BẢN (incremental update): giữ nguyên từng byte của file gốc
    /// rồi ghi thêm một bản sửa đổi mới vào cuối, trong đó có chỗ trống /Contents và hai dải /ByteRange.
    ///
    /// Bắt buộc phải nối bản vì chữ ký PDF phủ OFFSET BYTE TUYỆT ĐỐI: ghi lại cả tài liệu khiến mọi byte dịch
    /// chỗ, /ByteRange của chữ ký CŨ trỏ sang byte khác và chữ ký cũ chết.
    ///
    /// Đây là NỬA ĐẦU của luồng ký: hàm này dừng lại ngay khi đã biết dãy byte cần ký, vì việc ký nằm ở token
    /// trên máy người dùng. Nửa sau là <see cref="IPdfContentWriter"/>.
    /// </summary>
    public interface IPdfPreparer
    {
        /// <summary>
        /// Trả về bản đã nối kèm hai dải /ByteRange và dãy byte cần ký. Chưa có chữ ký nào được ghi vào:
        /// chỗ /Contents còn nguyên phần đệm '0'.
        /// </summary>
        PdfPreparedDto Prepare(byte[] pdf, PdfPrepareOptionsDto options);

        /// <summary>
        /// Xếp toàn bộ object của bản mới theo đúng thứ tự ghi ra file. Dict giá trị chữ ký nằm CUỐI CÙNG để
        /// khâu vá dò ngược từ cuối file là chắc chắn gặp chỗ trống của bản mới, không vớ phải chữ ký có sẵn.
        /// </summary>
        List<PdfAppearanceObjectDto> BuildRevisionObjects(PdfRevisionDto revision, string catalogRaw,
            PdfAnnotationPlanDto plan, IReadOnlyList<PdfAnnotationDto> chuKyWidgets,
            IReadOnlyList<PdfAnnotationDto> annotationsKhac, int sigFieldNo, int sigValueNo,
            PdfPrepareOptionsDto options);

        /// <summary>Field chữ ký: gộp widget khi chỉ có một khối, hoặc field cha kèm các widget con.</summary>
        List<PdfAppearanceObjectDto> BuildSignatureField(IReadOnlyList<PdfAnnotationDto> chuKyWidgets,
            int sigFieldNo, int sigValueNo);

        /// <summary>
        /// Khối ảnh KHÔNG nhồi chữ ký số: vẫn vẽ lên trang nhưng là annotation thường, bấm vào không ra bảng
        /// thông tin chữ ký.
        /// </summary>
        string BuildStampAnnotation(PdfAnnotationDto annotation);

        /// <summary>
        /// Dict giá trị chữ ký. /ByteRange và /Contents ghi bằng chỗ trống bề rộng CỐ ĐỊNH rồi vá đè sau —
        /// vá xong độ dài không đổi nên mọi offset đã tính vẫn đúng.
        /// </summary>
        string BuildSignatureValue(PdfPrepareOptionsDto options);

        /// <summary>Chỗ trống /ByteRange: bốn ô bề rộng cố định, vá đè bằng số thật khi đã biết offset.</summary>
        string BuildByteRangePlaceholder();

        /// <summary>Ghép bản mới vào sau file gốc rồi vá /ByteRange bằng offset thật.</summary>
        PdfPreparedDto WriteRevision(byte[] pdf, PdfRevisionDto revision,
            IReadOnlyList<PdfAppearanceObjectDto> objects, int xrefObjectNumber);

        /// <summary>
        /// Vá /ByteRange và cắt ra dãy byte cần ký. Vùng 1 kết thúc TẠI dấu '&lt;' mở /Contents, vùng 2 bắt đầu
        /// NGAY SAU dấu '&gt;' đóng nó.
        /// </summary>
        PdfPreparedDto PatchByteRange(byte[] output, int baseLength);

        /// <summary>
        /// Duyệt cây trang lấy số hiệu object của từng trang theo đúng thứ tự đọc. Không lấy thẳng kid đầu
        /// tiên: /Kids của node gốc có thể lại là node /Pages trung gian chứ không phải trang.
        /// </summary>
        List<int> ReadPageOrder(PdfRevisionDto revision, int pagesObjectNumber);

        /// <summary>
        /// Đọc /MediaBox của một trang, chuẩn hoá về (trái, dưới, rộng, cao). Trang không tự khai thì THỪA KẾ
        /// của node /Pages cha — bỏ qua chỗ thừa kế này là tính nhầm khổ giấy và chữ ký lệch hẳn khỏi vùng nhìn.
        /// </summary>
        PdfRectPointsDto ReadPageBox(PdfRevisionDto revision, string pageRaw, int pagesObjectNumber);

        /// <summary>
        /// Chốt các khối sẽ vẽ lên trang theo hai cờ của template và dựng sẵn mặt chữ ký cho từng khối.
        /// Không khối nào hiển thị thì vẫn phát hành MỘT chữ ký vô hình — chữ ký vẫn hợp lệ, chỉ không vẽ gì.
        /// </summary>
        PdfAnnotationPlanDto PlanAnnotations(PdfRevisionDto revision, IReadOnlyList<int> pageOrder,
            int pagesObjectNumber, PdfPrepareOptionsDto options, int nextObjectNumber);

        /// <summary>
        /// Quy đổi toạ độ tỉ lệ 0..1 (gốc TRÊN-TRÁI, Y hướng xuống) sang hệ toạ độ PDF (gốc DƯỚI-TRÁI, Y hướng
        /// lên). Phép đổi hệ quy chiếu chỉ tồn tại ở đúng một chỗ này, không rải ra nơi khác.
        /// </summary>
        PdfRectPointsDto ToPoints(PdfPlacementDto placement, PdfRectPointsDto pageBox);

        /// <summary>
        /// Áp sàn kích cỡ cho khối chữ ký số: co giãn theo khổ trang so với khổ tham chiếu, rồi kéo lên sàn
        /// nếu người dùng kéo ô nhỏ tới mức không đọc nổi chữ. Vị trí người dùng chọn giữ nguyên.
        /// </summary>
        PdfRectPointsDto ApplyChuKyMinSize(PdfRectPointsDto rect, PdfRectPointsDto pageBox);

        /// <summary>
        /// Nắn ô chữ ký tươi cho khớp ảnh: GIỮ NGUYÊN TỈ LỆ khung hình để nét chữ không bị bóp méo, và chặn
        /// trần bề rộng theo <c>SealPlacementConstants.MaxChuKyTuoiWidthRatio</c> — ảnh scan có thể rất lớn,
        /// không có trần thì chữ ký chiếm hết nửa trang. Ô mới lấy đúng TÂM của ô người dùng đã đặt.
        /// </summary>
        PdfRectPointsDto ApplyChuKyTuoiFit(PdfRectPointsDto rect, PdfRectPointsDto pageBox, byte[] anh);

        /// <summary>Kẹp ô vào trong trang bằng cách DỊCH, không thu nhỏ.</summary>
        PdfRectPointsDto ClampToPage(PdfRectPointsDto rect, PdfRectPointsDto pageBox);

        /// <summary>
        /// Ghi đè Catalog: thêm field chữ ký vào /AcroForm /Fields và bật /SigFlags 3. Xử lý cả ba dạng gặp
        /// thực tế: /AcroForm là dict nằm thẳng, là tham chiếu gián tiếp, hoặc chưa có (file chưa từng ký).
        /// </summary>
        string BuildCatalogOverride(string catalogRaw, int signatureFieldNumber);

        /// <summary>
        /// Dựng LUỒNG xref mới (/Type/XRef), không nén và không predictor. /Prev trỏ về xref cũ, /ID giữ
        /// nguyên của tài liệu — phần tử đầu của /ID là định danh vĩnh viễn, đổi nó là thành tài liệu khác.
        /// </summary>
        string BuildXrefStream(PdfRevisionDto revision, int xrefObjectNumber,
            IReadOnlyDictionary<int, long> offsets, int newSize);

        /// <summary>
        /// Dựng BẢNG xref cổ điển + trailer. Xref mới BẮT BUỘC cùng dạng với xref cũ nó nối vào, mà PDF đời cũ
        /// trong kho lưu trữ vẫn dùng dạng bảng.
        /// </summary>
        string BuildXrefTable(PdfRevisionDto revision, IReadOnlyDictionary<int, long> offsets, int newSize,
            long xrefStart);
    }
}
