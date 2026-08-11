using System.Text;

namespace ksts.be.external.Pdf.Interfaces
{
    /// <summary>
    /// Bộ ghi object PDF TỐI THIỂU — chỉ đủ những kiểu giá trị thực sự xuất hiện trong các object mà bản ký
    /// nối bản tự sinh ra: dict chữ ký, field chữ ký, widget, luồng xref.
    ///
    /// Phạm vi hẹp là CÓ CHỦ ĐÍCH, đây không phải bộ ghi PDF tổng quát. Object CŨ bị ghi đè (Catalog, trang)
    /// không đi qua đây: chúng được CHÈN BYTE vào đúng nguyên văn dict gốc. Phân tích rồi dựng lại một dict lạ
    /// là tự chuốc rủi ro làm hỏng những khoá mình không lường trước; giữ nguyên văn thì luôn đúng.
    /// </summary>
    public interface IPdfObjectWriter
    {
        /// <summary>Ghi trọn một object gián tiếp: "N 0 obj &lt;giá trị&gt; endobj".</summary>
        void WriteObject(StringBuilder builder, int number, object? value);

        /// <summary>
        /// Ghi một giá trị. Kiểu không nằm trong danh sách hỗ trợ thì NÉM chứ không ghi bừa — một giá trị ghi
        /// sai lặng lẽ sẽ thành file PDF hỏng mà không ai lần ra hỏng ở đâu.
        /// </summary>
        void WriteValue(StringBuilder builder, object? value);

        /// <summary>
        /// Ghi chuỗi. Toàn ASCII thì dùng literal "(...)". Có ký tự ngoài ASCII (tên người ký tiếng Việt, lý do
        /// ký) thì BẮT BUỘC hex UTF-16BE kèm BOM FEFF: bảng mã mặc định của PDF không có ký tự có dấu, ghi
        /// literal là mất dấu hoặc ra ký tự lạ.
        /// </summary>
        void WriteString(StringBuilder builder, string value);

        /// <summary>
        /// Chèn thêm một tham chiếu vào CUỐI mảng tên <paramref name="key"/> trong nguyên văn dict, giữ nguyên
        /// từng byte còn lại. Không có mảng đó thì thêm mới trước "&gt;&gt;".
        /// Trả về null khi mảng là tham chiếu gián tiếp — tầng trên phải ghi đè chính object đó.
        /// </summary>
        string? AppendToArray(string dictRaw, string key, int newObjectNumber);
    }
}
