using Microsoft.AspNetCore.Http;

namespace ksts.be.external.Storage.Interfaces
{
    /// <summary>
    /// Chỗ lưu PDF nguồn và PDF đã ký của một lô, trên MinIO theo tiền tố <c>lo-ky/{loKyId}/</c>.
    ///
    /// Không giữ file trên đĩa máy chạy API: API có thể chạy nhiều instance hoặc trong container không có ổ
    /// bền vững, mà MinIO là chỗ duy nhất mọi instance nhìn thấy chung.
    /// </summary>
    public interface ILoKyFileStorage
    {
        /// <summary>Lưu một file nguồn người dùng tải lên, trả về object key do SERVER đặt.</summary>
        Task<string> LuuFileNguonAsync(int loKyId, int thuTu, IFormFile file,
            CancellationToken cancellationToken = default);

        /// <summary>Lưu bản đã ký, trả về object key.</summary>
        Task<string> LuuFileDaKyAsync(int loKyId, int thuTu, byte[] noiDung,
            CancellationToken cancellationToken = default);

        /// <summary>Tải nội dung một object của lô về bộ nhớ.</summary>
        Task<byte[]> TaiAsync(string objectKey, CancellationToken cancellationToken = default);

        /// <summary>Dọn sạch mọi file của một lô — tách theo loKyId nên xoá gọn cả thư mục.</summary>
        Task XoaLoAsync(int loKyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dọn MỘT thư mục con của lô (<c>nguon</c> hoặc <c>da-ky</c>). Xoá theo tiền tố của chính lô nên
        /// KHÔNG bao giờ đụng tới file gốc người dùng chọn từ thư mục có sẵn trên kho: chế độ đó không chép
        /// gì vào <c>lo-ky/</c>, tiền tố này rỗng và lệnh xoá thành vô hại.
        /// </summary>
        Task XoaThuMucAsync(int loKyId, string thuMuc, CancellationToken cancellationToken = default);

        /// <summary>
        /// Object key do server đặt theo thứ tự file trong lô, KHÔNG lấy tên file người dùng tải lên: tên
        /// trùng sẽ ghi đè lẫn nhau, mà tên người dùng còn có thể chứa ký tự phá đường dẫn.
        /// </summary>
        string BuildObjectKey(int loKyId, string thuMuc, int thuTu);
    }
}
