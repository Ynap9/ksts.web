using ksts.be.external.Storage.Dtos;
using Microsoft.AspNetCore.Http;

namespace ksts.be.external.Storage.Interfaces
{
    /// <summary>
    /// Lưu ảnh dấu đỏ / chữ ký tươi của template lên kho object, kèm kiểm tra ràng buộc ảnh.
    ///
    /// Bọc thêm một lớp trên <see cref="IS3FileStorage"/> để tầng nghiệp vụ không phải tự dựng object key và
    /// tự nhớ luật kiểm ảnh - hai thứ lặp lại y hệt ở cả tạo mới lẫn cập nhật template.
    /// </summary>
    public interface ITemplateImageStorage
    {
        /// <summary>
        /// Kiểm ảnh rồi đẩy lên kho, dọn sạch bản cũ trước khi ghi.
        ///
        /// Hai đường dọn khác nhau: <paramref name="oldObjectKey"/> khác key mới thì xoá theo key cũ — key
        /// mang đuôi file nên đổi ảnh .png sang .jpg là sinh key khác, không xoá thì bỏ lại object mồ côi;
        /// còn key mới mà đã có sẵn vật trên kho thì xoá vật đó rồi mới ghi.
        /// </summary>
        Task<S3UploadResultDto> SaveAsync(IFormFile file, int templateId, string objectName,
            string? oldObjectKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ảnh còn nằm trên kho hay không. Key rỗng trả false.
        ///
        /// Cần vì DB chỉ giữ một CHUỖI đường dẫn: đổi bucket hay dọn kho bằng tay là template vẫn khai có
        /// ảnh trong khi vật đã biến mất, màn cấu hình lại hiện đúng ô ảnh đó nên người dùng tưởng còn.
        /// </summary>
        Task<bool> TonTaiAsync(string? objectKey, CancellationToken cancellationToken = default);

        /// <summary>Xoá một ảnh theo object key. Key rỗng thì không làm gì.</summary>
        Task RemoveAsync(string? objectKey, CancellationToken cancellationToken = default);

        /// <summary>Xoá toàn bộ ảnh của một template - dùng khi xoá template.</summary>
        Task RemoveAllAsync(int templateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm đuôi file và dung lượng theo TemplateConstants. Sai thì ném
        /// UserFriendlyException(TemplateImageInvalid).
        /// </summary>
        void ValidateImage(IFormFile file);
    }
}
