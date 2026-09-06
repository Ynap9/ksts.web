using kssm.be.external.S3.Dtos;
using Microsoft.AspNetCore.Http;

namespace kssm.be.external.S3.Interfaces
{
    /// <summary>
    /// Kho object của luồng ký. Khác <see cref="IS3FileStorage"/> ở chỗ mọi thao tác đều nhận cấu hình kho:
    /// lô file tải lên dùng kho mặc định, còn lô của dự án đọc và ghi ngay trên MinIO của chính dự án đó.
    /// </summary>
    public interface ILoKyFileStorage
    {
        /// <summary>Đẩy một file người dùng tải lên vào kho, giữ nguyên nội dung.</summary>
        Task<long> SaveSourceAsync(S3ConnectionDto storage, IFormFile file, string objectKey,
            CancellationToken cancellationToken = default);

        /// <summary>Đẩy nội dung đã nằm trong bộ nhớ lên kho — dùng cho bản PDF vừa ký xong.</summary>
        Task UploadAsync(S3ConnectionDto storage, byte[] content, string objectKey, string contentType,
            CancellationToken cancellationToken = default);

        /// <summary>Tải nội dung một object về bộ nhớ.</summary>
        Task<byte[]> DownloadAsync(S3ConnectionDto storage, string objectKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Liệt kê mọi object key dưới một tiền tố, ĐI HẾT các trang kết quả. S3 trả tối đa 1000 key mỗi lượt
        /// kèm token trang sau; dừng ở lượt đầu là lô 5000 file chỉ thấy được một phần năm.
        /// </summary>
        Task<IReadOnlyList<string>> ListKeysAsync(S3ConnectionDto storage, string prefix,
            CancellationToken cancellationToken = default);

        /// <summary>Xoá mọi object dưới một tiền tố. Key không tồn tại không bị coi là lỗi.</summary>
        Task DeleteByPrefixAsync(S3ConnectionDto storage, string prefix,
            CancellationToken cancellationToken = default);
    }
}
