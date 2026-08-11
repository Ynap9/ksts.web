using ksts.be.applications.GiayBao.Dtos;
using ksts.be.external.Excel.Dtos;
using ksts.be.external.Jobs.Dtos;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace ksts.be.applications.GiayBao.Interfaces
{
    /// <summary>
    /// In giấy báo trúng tuyển hàng loạt: đọc danh sách thí sinh từ Excel, nhồi vào mẫu HTML, chuyển sang
    /// PDF rồi gói thành một file nén để tải về.
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

        /// <summary>Dựng toàn bộ giấy báo của lô rồi ghi ra file nén. Lỗi một file không làm dừng cả lô.</summary>
        Task ChayLoAsync(string jobId, string template, List<Dictionary<string, string>> hopLe);

        /// <summary>
        /// Mở việc đẩy cả lô lên kho object chạy nền và trả về ngay trạng thái lô.
        ///
        /// Độc lập hoàn toàn với việc tải zip về: lô đã dựng xong thì đẩy lên kho hay tải về máy là hai lựa
        /// chọn riêng, làm một trong hai hay cả hai đều được và không việc nào chặn việc nào.
        /// </summary>
        ZipJobDto BatDauDayLenKho(string jobId);

        /// <summary>
        /// Đẩy từng giấy báo trong file nén của lô lên kho object, nhiều file song song. Lỗi một file không
        /// làm dừng cả lô, giống hệt khâu dựng.
        /// </summary>
        Task ChayDayLenKhoAsync(string jobId, string zipPath);

        /// <summary>
        /// Đẩy một giấy báo lên kho. Nuốt lỗi rồi đếm vào <c>SoLoiDayLenKho</c> chứ không ném ra: một file
        /// hỏng không được phép làm dừng cả lô đang đẩy.
        /// </summary>
        Task DayMotFileAsync(string jobId, ZipArchive archive, string tenFile);
    }
}
