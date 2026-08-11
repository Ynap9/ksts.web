using ksts.be.external.Storage.Dtos;
using Microsoft.AspNetCore.Http;

namespace ksts.be.external.Storage.Interfaces
{
    /// <summary>
    /// Kho object MinIO (S3-compatible) cho file của KSTS.
    ///
    /// Object key do CALLER quyết định chứ không lấy tên file người dùng tải lên: hai người cùng đặt tên
    /// "dau-do.png" sẽ ghi đè ảnh của nhau.
    /// </summary>
    public interface IS3FileStorage
    {
        /// <summary>
        /// Đẩy file lên kho với đúng <paramref name="objectKey"/> chỉ định, ghi đè nếu key đã tồn tại.
        /// Object được đặt quyền đọc công khai để FE hiển thị ảnh trực tiếp bằng URL.
        /// </summary>
        Task<S3UploadResultDto> UploadAsync(IFormFile file, string objectKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đẩy nội dung đã nằm sẵn trong bộ nhớ lên kho - dùng cho file do server sinh ra (bản PDF đã ký),
        /// không đi qua IFormFile.
        /// </summary>
        Task<S3UploadResultDto> UploadBytesAsync(byte[] noiDung, string objectKey, string contentType,
            CancellationToken cancellationToken = default);

        /// <summary>Tải nội dung một object về bộ nhớ. Key không tồn tại thì ném StorageDownloadFailed.</summary>
        Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Object có thật trên kho hay không, hỏi bằng một lần đọc metadata chứ không tải nội dung về.
        ///
        /// Cần thiết vì đường dẫn lưu trong DB chỉ là một CHUỖI: đổi bucket hay dọn kho bằng tay là DB vẫn
        /// trỏ vào một object đã biến mất, mà chỉ tới lúc dùng mới vỡ ra.
        /// </summary>
        Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Liệt kê mọi object key nằm dưới một tiền tố, ĐI HẾT các trang. S3 trả tối đa 1000 key một lượt
        /// kèm token trang sau; dừng ở lượt đầu là lô 5000 file chỉ thấy được một phần năm.
        /// </summary>
        Task<IReadOnlyList<string>> ListKeysAsync(string keyPrefix, CancellationToken cancellationToken = default);

        /// <summary>
        /// Chép một object sang key khác NGAY TRONG kho, không kéo nội dung về server rồi đẩy ngược lên.
        /// Lô vài nghìn file là vài GB đi qua đường truyền hai lần nếu làm kiểu tải-về-đẩy-lên.
        /// </summary>
        Task CopyAsync(string objectKeyNguon, string objectKeyDich,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xoá một object. Key không tồn tại KHÔNG bị coi là lỗi - S3 xoá là thao tác idempotent, và bên gọi
        /// thường xoá "phòng xa" trước khi ghi bản mới.
        /// </summary>
        Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

        /// <summary>Xoá mọi object nằm dưới một tiền tố - dùng khi xoá template thì dọn sạch ảnh của nó.</summary>
        Task DeleteByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);

        /// <summary>URL công khai của một object, dạng {S3_URL}/{bucket}/{objectKey}.</summary>
        string BuildPublicUrl(string objectKey);
    }
}
