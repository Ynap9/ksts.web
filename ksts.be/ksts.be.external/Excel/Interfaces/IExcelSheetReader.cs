using ClosedXML.Excel;
using ksts.be.external.Excel.Dtos;

namespace ksts.be.external.Excel.Interfaces
{
    /// <summary>
    /// Đọc file Excel thành các dòng khoá theo tiêu đề đã chuẩn hoá.
    /// Khoá theo tên cột chứ không theo thứ tự để file cùng bộ tiêu đề vẫn đọc đúng khi đảo hoặc thừa cột.
    /// </summary>
    public interface IExcelSheetReader
    {
        /// <summary>Liệt kê sheet kèm số dòng dữ liệu để người dùng chọn trước khi in.</summary>
        List<ExcelSheetInfoDto> ListSheets(Stream stream);

        /// <summary>
        /// Đọc một sheet; bỏ trống <paramref name="sheetName"/> thì lấy sheet đầu tiên.
        /// <paramref name="startRow"/> là DÒNG TIÊU ĐỀ, dữ liệu tính từ dòng kế tiếp - file thật hay có
        /// mấy dòng tên đơn vị hoặc chú thích phía trên bảng.
        /// Ô ngày trả về dạng dd/MM/yyyy, ô còn lại lấy đúng chuỗi hiển thị trong Excel để không làm sai
        /// lệch điểm số hay số CCCD. File không đọc được ném UserFriendlyException(ExcelUnreadable).
        /// </summary>
        ExcelSheetDto ReadSheet(Stream stream, string? sheetName, int startRow);

        /// <summary>Chuẩn hoá tiêu đề: bỏ dấu, bỏ ký tự không phải chữ số, về chữ thường.</summary>
        string NormalizeKey(string? text);

        /// <summary>
        /// Lấy giá trị của ô theo danh sách tên cột ứng viên, dùng cái đầu tiên có mặt và có dữ liệu.
        /// Cùng một trường nhưng mỗi đợt kết xuất đặt tên cột một khác, nên phải nhận nhiều tên gọi thay vì
        /// bắt người dùng sửa tiêu đề trong file. Không cột nào khớp thì trả chuỗi rỗng.
        /// </summary>
        string LayGiaTri(IReadOnlyDictionary<string, string> row, IEnumerable<string> tenCotUngVien);

        /// <summary>Mở workbook, quy mọi lỗi định dạng về một thông báo người dùng hiểu được.</summary>
        XLWorkbook MoWorkbook(Stream stream);
    }
}
