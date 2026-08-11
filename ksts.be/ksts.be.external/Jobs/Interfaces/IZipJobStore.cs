using ksts.be.external.Jobs.Dtos;

namespace ksts.be.external.Jobs.Interfaces
{
    /// <summary>
    /// Kho trạng thái các lô dựng file nén đang chạy nền. Giữ trong bộ nhớ vì đây là tiến độ tạm của một
    /// phiên làm việc, không phải dữ liệu nghiệp vụ cần lưu lại.
    /// </summary>
    public interface IZipJobStore
    {
        /// <summary>Mở một lô mới, trả về bản ghi kèm JobId và token tải.</summary>
        ZipJobDto Tao(int tongSo);

        ZipJobDto? Lay(string jobId);

        /// <summary>Cập nhật tiến độ theo cách an toàn khi nhiều luồng cùng ghi.</summary>
        void CapNhat(string jobId, Action<ZipJobDto> thayDoi);

        /// <summary>Xoá lô đã quá hạn kèm file nén của nó — không dọn thì đĩa đầy sau vài lô.</summary>
        void DonHetHan();
    }
}
