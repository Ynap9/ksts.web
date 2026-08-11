using ksts.be.external.Images.Dtos;

namespace ksts.be.external.Images.Interfaces
{
    /// <summary>
    /// Đo KÍCH THƯỚC THẬT của ảnh: số pixel và độ phân giải (DPI) khai trong metadata, quy ra point của hệ
    /// toạ độ PDF.
    ///
    /// Sinh ra để phục vụ đúng một yêu cầu nghiệp vụ: CON DẤU KHÔNG ĐƯỢC RESIZE. Dấu cơ quan có kích thước
    /// thật, nên phải vẽ đúng cỡ gốc chứ không co theo khổ trang hay theo khung người dùng kéo.
    ///
    /// Tự đọc header thay vì dùng thư viện ảnh: chỉ cần width/height/DPI của PNG và JPEG - hai định dạng duy
    /// nhất template chấp nhận - nên không đáng kéo thêm một phụ thuộc có ràng buộc bản quyền.
    /// </summary>
    public interface IImageSizeReader
    {
        /// <summary>
        /// Đọc kích thước ảnh. Ảnh không khai DPI thì dùng
        /// <see cref="shared.Constants.Signing.SealPlacementConstants.DefaultImageDpi"/> và bật cờ
        /// <see cref="ImageSizeDto.HasDpiMetadata"/> = false để tầng trên cảnh báo được cho người dùng.
        /// Không nhận diện được định dạng thì ném UserFriendlyException(SealImageUnreadable).
        /// </summary>
        ImageSizeDto Read(byte[] bytes);

        /// <summary>Đọc header PNG (IHDR cho kích thước, pHYs cho DPI). Trả null nếu không phải PNG.</summary>
        ImageSizeDto? TryReadPng(byte[] bytes);

        /// <summary>Đọc header JPEG (SOFn cho kích thước, APP0/JFIF cho DPI). Trả null nếu không phải JPEG.</summary>
        ImageSizeDto? TryReadJpeg(byte[] bytes);
    }
}
