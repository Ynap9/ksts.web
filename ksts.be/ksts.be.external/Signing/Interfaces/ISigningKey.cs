using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Signing.Interfaces
{
    /// <summary>
    /// Nguồn thực hiện phép KÝ trên một dãy byte.
    ///
    /// Khoá bí mật nằm trong chip token cắm ở máy người dùng và không trích xuất được, nên phép ký phải chạy
    /// ở đó; qua mạng chỉ có SignedAttributes và chữ ký thô. Implement mặc định là
    /// <see cref="Implements.PluginSigningKey"/>; bản đọc certificate store của máy chạy API chỉ dùng được
    /// khi API và token nằm trên cùng một máy Windows. Xem .claude/docs/ky-so-web-vs-desktop.md.
    ///
    /// Mọi phép ký gắn với MỘT lô: phiên ký mở theo lô, và máy người dùng phải nộp chứng thư trước khi lô
    /// bắt đầu.
    /// </summary>
    public interface ISigningKey
    {
        /// <summary>Chứng thư của phiên ký. Chỉ cần phần CÔNG KHAI để dựng chuỗi tin cậy và lắp vào CMS.</summary>
        Task<X509Certificate2> LayChungThuAsync(int loKyId, string thumbprint,
            CancellationToken cancellationToken);

        /// <summary>
        /// Ký SHA-256 lên <paramref name="duLieu"/> — chính là dãy byte SignedAttributes. Trả về chữ ký thô,
        /// chưa bọc CMS.
        /// </summary>
        Task<byte[]> KyAsync(int loKyId, byte[] duLieu, X509Certificate2 cert,
            CancellationToken cancellationToken);

        /// <summary>Chuỗi chứng thư dựng từ cert người ký, nhúng vào CMS để về sau verify được offline.</summary>
        IReadOnlyList<X509Certificate2> LayChuoiChungThu(X509Certificate2 cert);
    }
}
