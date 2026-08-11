using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Signing.Interfaces
{
    /// <summary>
    /// Nguồn thực hiện phép KÝ trên một dãy byte.
    ///
    /// Implement hiện tại dùng khoá trong certificate store của MÁY ĐANG CHẠY API — đúng như
    /// <see cref="Certificates.Interfaces.ICertificateProvider"/>, và cũng chỉ đúng ở môi trường dev. Đích đến
    /// là plugin ở máy người dùng: khoá nằm trong chip token, không trích xuất được, nên qua mạng chỉ có hash
    /// và chứng thư phần công khai. Tách interface ở đây chính là để đổi nguồn ký mà KHÔNG phải sửa service
    /// lẫn controller. Xem .claude/docs/ky-so-web-vs-desktop.md.
    /// </summary>
    public interface ISigningKey
    {
        /// <summary>Chứng thư theo vân tay, kèm khoá riêng dùng được để ký.</summary>
        X509Certificate2 LayChungThu(string thumbprint);

        /// <summary>
        /// Ký SHA-256 lên <paramref name="duLieu"/> — chính là dãy byte SignedAttributes mà về sau plugin sẽ
        /// nhận và ký bằng token. Trả về chữ ký thô, chưa bọc CMS.
        /// </summary>
        byte[] Ky(byte[] duLieu, X509Certificate2 cert);

        /// <summary>Chuỗi chứng thư dựng từ cert người ký, nhúng vào CMS để về sau verify được offline.</summary>
        IReadOnlyList<X509Certificate2> LayChuoiChungThu(X509Certificate2 cert);
    }
}
