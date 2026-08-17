using ksts.be.external.Colors.Dtos;
using ksts.be.external.Pdf.Dtos;
using PdfSharp.Drawing;

namespace ksts.be.external.Pdf.Interfaces
{
    /// <summary>
    /// Dựng mặt chữ ký (appearance stream) rồi CẤY sang tài liệu đích dưới dạng Form XObject.
    ///
    /// Vẫn dùng PDFsharp ở riêng chỗ này trong khi phần ghi file đã tự làm, vì tên người ký có DẤU TIẾNG VIỆT:
    /// font base-14 với bảng mã WinAnsi không mã hoá được, buộc phải nhúng một font Unicode đã rút gọn — việc
    /// mà PDFsharp làm đúng sẵn. Cách làm: vẽ vào một tài liệu PDF tạm trong bộ nhớ có khổ trang đúng bằng
    /// khối chữ ký, rồi bê luồng nội dung của trang đó thành Form XObject kèm toàn bộ cây tài nguyên.
    /// </summary>
    public interface IPdfAppearanceBuilder
    {
        /// <summary>
        /// Khối chữ ký số hai dòng (tên người ký + giờ ký). Mọi số đo bên trong được nhân theo tỉ lệ so với
        /// khổ chuẩn nên ô to thì chữ to theo, không bị chữ giữ nguyên cỡ rồi lọt thỏm hoặc tràn khỏi hộp.
        /// </summary>
        PdfAppearanceDto BuildText(string dong1, string dong2, int firstObjectNumber, double width,
            double height, string mau);

        /// <summary>
        /// Mặt chữ ký là chính ẢNH CHỮ KÝ TƯƠI, không vẽ chữ. Ảnh được vẽ kín ô đã tính sẵn theo đúng tỉ lệ
        /// khung hình của nó ở tầng trên, ở đây chỉ vẽ trải đúng khổ ô.
        /// </summary>
        PdfAppearanceDto BuildImage(byte[] anh, int doDamPhanTram, int doDayNetPhanTram, int firstObjectNumber,
            double width, double height, string? mau);

        /// <summary>
        /// Các lớp ảnh cần vẽ để nong nét chữ ký dày lên: một vòng lớp lệch quanh tâm rồi lớp lõi vẽ sau cùng.
        ///
        /// Đây là phần "nét đậm" mà <see cref="ApDoDamVaMau"/> không làm được: /Decode ánh xạ lại độ sáng từng
        /// pixel TẠI CHỖ nên nét mảnh chỉ sẫm màu hơn chứ không dày thêm. Muốn dày thì mực phải lan sang pixel
        /// bên cạnh, và cách rẻ nhất trong PDF là vẽ lại chính ảnh đó vài lần lệch đi một chút — mọi lệnh vẽ
        /// đều trỏ về cùng một XObject nên file không phình.
        ///
        /// Lõi thu vào đúng bằng bán kính nong để vòng lệch không tràn ra ngoài BBox của mặt chữ ký, và được
        /// xếp CUỐI danh sách để nét gốc nằm trên cùng, sắc nét, không bị các lớp lệch làm nhoè.
        /// </summary>
        IReadOnlyList<PdfRectPointsDto> TinhLopNongNet(double width, double height, int doDayNetPhanTram);

        /// <summary>
        /// Chỉnh độ đậm VÀ màu mực của ảnh bằng chính siêu dữ liệu ảnh trong PDF thay vì xử lý từng pixel rồi
        /// mã hoá lại: không kéo thêm thư viện ảnh, không tốn CPU, và byte ảnh gốc giữ nguyên nên không mất nét.
        ///
        /// Độ đậm đi bằng mảng /Decode — nó ánh xạ tuyến tính mỗi thành phần màu, đặt [(1-f)/2, (1+f)/2] với f
        /// là hệ số đậm thì ra đúng phép tăng tương phản quanh mức xám giữa, cùng công thức bản xem trước FE
        /// đang dùng. Màu mực đi kèm trong cùng phép tuyến tính đó: điểm tối nhất kéo về màu đã chọn, điểm
        /// sáng nhất vẫn là trắng, nên nền trắng của ảnh quét không bị nhuộm theo.
        ///
        /// CHƯA CHỌN màu (chuỗi rỗng hoặc null) nghĩa là giữ nguyên mực của ảnh gốc: phép ánh xạ khi đó rút
        /// gọn về đúng phần độ đậm. Đen là một màu thật như mọi màu khác, không phải cờ giữ nguyên.
        /// </summary>
        void ApDoDamVaMau(PdfAppearanceDto appearance, int doDamPhanTram, string? mau);

        /// <summary>
        /// Mảng /Decode cho ảnh có <paramref name="soThanhPhan"/> kênh màu: kênh nào cũng đi từ điểm tối (đã
        /// nhuộm màu) tới điểm sáng. Ảnh CMYK chỉ nhận phần độ đậm vì ở đó giá trị 0 là KHÔNG mực chứ không
        /// phải màu tối, nhuộm theo cùng công thức sẽ ra ảnh âm bản.
        /// </summary>
        string BuildDecodeArray(double can, double tran, int soThanhPhan, RgbColorDto? mau);

        /// <summary>
        /// Bảng màu /Indexed thay cho /DeviceGray khi ảnh thang xám cần nhuộm màu: /Decode một kênh chỉ ra
        /// được sắc xám, muốn ra màu thì phải đổi hẳn không gian màu. Mỗi ô của bảng là một mức xám gốc đã
        /// đi qua cả độ đậm lẫn màu mực, nên ảnh vẫn giữ nguyên byte dữ liệu và không cần /Decode nữa.
        /// </summary>
        string BuildIndexedPalette(double can, double tran, int hival, RgbColorDto mau);

        /// <summary>Số bit mỗi thành phần màu của ảnh; mặc định 8 khi dict không khai.</summary>
        int ReadBitsPerComponent(string dictText);

        /// <summary>
        /// Ghi một thành phần màu ra chuỗi cho PDF: kẹp về 0..1 và dùng dấu chấm thập phân bất kể máy chủ đặt
        /// vùng nào — dấu phẩy lọt vào mảng /Decode là hỏng cú pháp file.
        /// </summary>
        string FormatColorValue(double giaTri);

        /// <summary>
        /// Số thành phần màu của một ảnh theo /ColorSpace. Trả 0 khi không chắc (bảng màu /Indexed, hoặc
        /// không gian màu là tham chiếu gián tiếp) — lúc đó KHÔNG đụng vào ảnh, vì đặt /Decode sai số phần tử
        /// là làm hỏng màu chứ không phải chỉnh đậm nhạt.
        /// </summary>
        int DemThanhPhanMau(string dictText);

        /// <summary>
        /// Vẽ ra một tài liệu PDF tạm một trang, khổ trang đúng bằng khối chữ ký. Không nén luồng nội dung để
        /// khâu đọc lại bên dưới đơn giản và chắc chắn hơn. Có khoá vì PDFsharp giữ bộ nhớ đệm font dùng
        /// chung cho cả tiến trình, mà nhiều file được ký song song.
        /// </summary>
        byte[] Draw(double width, double height, Action<XGraphics, XRect> ve);

        /// <summary>Phần vẽ thật, đã nằm trong khoá của <see cref="Draw"/>.</summary>
        byte[] VeVaoTaiLieuTam(double width, double height, Action<XGraphics, XRect> ve);

        /// <summary>
        /// Bê trang đầu của một tài liệu PDF tạm thành Form XObject, kéo theo cây tài nguyên (font, font
        /// descriptor, file font nhúng) và đánh lại số hiệu cho khớp tài liệu đích.
        /// </summary>
        PdfAppearanceDto Transplant(byte[] tempPdf, int firstObjectNumber, double width, double height);

        /// <summary>Lấy nguyên văn /Resources của trang tạm; nó có thể nằm thẳng hoặc là tham chiếu gián tiếp.</summary>
        string ReadResources(PdfRevisionDto temp, string pageBody);

        /// <summary>Liệt kê số hiệu của mọi tham chiếu gián tiếp "N G R" trong nguyên văn một dict.</summary>
        IEnumerable<int> FindRefs(string dictRaw);

        /// <summary>
        /// Đổi mọi "N G R" sang số hiệu đích theo <paramref name="map"/>, MỘT LƯỢT — thay nhiều lượt thì số
        /// vừa đổi có thể bị đổi tiếp thành số khác. Chỉ áp dụng cho phần DICT, không bao giờ cho thân stream:
        /// dữ liệu nội dung có thể chứa dãy số trông y hệt một tham chiếu.
        /// </summary>
        string Renumber(string dictRaw, IReadOnlyDictionary<int, int> map);
    }
}
