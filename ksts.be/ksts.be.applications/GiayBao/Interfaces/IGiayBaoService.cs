using ksts.be.applications.GiayBao.Dtos;
using ksts.be.external.Excel.Dtos;
using ksts.be.external.Jobs.Dtos;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace ksts.be.applications.GiayBao.Interfaces
{
    /// <summary>
    /// In giấy báo trúng tuyển hàng loạt: đọc danh sách thí sinh từ Excel, nhồi vào mẫu HTML, chuyển sang
    /// PDF rồi đẩy thẳng lên kho object.
    ///
    /// Máy chủ KHÔNG giữ file trên đĩa ở bất kỳ khâu nào. Lô 5000 giấy báo là gần 4 GB; gom vào một file nén
    /// tạm là đủ làm đầy ổ đĩa máy chủ, mà đầy ổ thì Gotenberg dựng file cũng hỏng theo. Muốn tải về máy thì
    /// file nén được dựng NGAY LÚC TẢI, kéo từng file từ kho ra rồi ghi thẳng vào luồng gửi cho trình duyệt.
    /// </summary>
    public interface IGiayBaoService
    {
        /// <summary>Danh sách sheet kèm số dòng để người dùng chọn sheet chứa thí sinh trước khi in.</summary>
        List<ExcelSheetInfoDto> DanhSachSheet(IFormFile file);

        /// <summary>
        /// Danh sách thí sinh trong sheet để hiển thị trước khi in; chỉ lấy số văn bản và họ tên.
        /// <paramref name="startRow"/> là dòng tiêu đề của bảng.
        /// </summary>
        List<ViewThiSinhDto> DanhSachThiSinh(IFormFile file, string? sheetName, int startRow);

        /// <summary>
        /// Mở một lô dựng giấy báo chạy nền và trả về ngay trạng thái lô. Đọc Excel xong là trả, phần dựng
        /// PDF chạy tiếp phía sau — giữ một request suốt 30 phút thì trình duyệt đã cắt kết nối từ lâu.
        /// </summary>
        ZipJobDto BatDauTaoZip(IFormFile file, string? sheetName, int startRow);

        /// <summary>
        /// Dựng toàn bộ giấy báo của lô, nhiều file song song, mỗi file dựng xong là đẩy ngay lên kho object
        /// rồi buông khỏi bộ nhớ. Lỗi một file không làm dừng cả lô.
        /// </summary>
        Task ChayLoAsync(string jobId, string template, List<Dictionary<string, string>> hopLe);

        /// <summary>
        /// Kéo giấy báo của lô từ kho về rồi nén thẳng vào <paramref name="dich"/> - là luồng gửi cho trình
        /// duyệt. Không có file nén trung gian trên đĩa máy chủ.
        ///
        /// Câu trả lời đã bắt đầu gửi đi thì không đổi được mã lỗi HTTP nữa, nên file nào kéo hỏng thì ghi
        /// log rồi bỏ qua chứ không làm hỏng cả gói.
        /// </summary>
        Task GhiNenAsync(string jobId, Stream dich, CancellationToken cancellationToken);

        /// <summary>Kéo một giấy báo từ kho về. Hỏng thì trả null để khâu nén bỏ qua file đó.</summary>
        Task<byte[]?> TaiMotFileAsync(string tenFile, CancellationToken cancellationToken);
    }
}
