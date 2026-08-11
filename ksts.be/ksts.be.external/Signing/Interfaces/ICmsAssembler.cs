using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Signing.Interfaces
{
    /// <summary>
    /// Dựng chữ ký CAdES detached cho PDF. Chữ ký do plugin ở máy người dùng ký rời nên phải ghép tay theo
    /// RFC 5652 — SignedCms.ComputeSignature chỉ dùng được khi khoá nằm ngay tại chỗ.
    /// </summary>
    public interface ICmsAssembler
    {
        /// <summary>
        /// Dựng SignedAttributes (DER, dạng SET OF) để plugin ký: contentType, messageDigest, signingTime,
        /// signingCertificateV2. Đây chính là chuỗi byte plugin băm SHA-256 rồi ký.
        /// </summary>
        byte[] BuildSignedAttributes(byte[] noiDungHash, X509Certificate2 cert, DateTime signedAtUtc);

        /// <summary>
        /// Ghép chữ ký thô của plugin thành CMS hoàn chỉnh, kèm token dấu thời gian làm unsigned attribute.
        /// <paramref name="chuoiChungThu"/> nhúng vào file để về sau verify được offline.
        /// </summary>
        byte[] Assemble(byte[] signedAttributes, byte[] chuKyTho, X509Certificate2 cert,
            IReadOnlyList<X509Certificate2> chuoiChungThu, byte[] tsaToken);

        /// <summary>Thuộc tính ESS signingCertificateV2 — .NET không tự thêm, thiếu thì không conform CAdES.</summary>
        byte[] BuildSigningCertificateV2(X509Certificate2 cert);

        /// <summary>Ghi một SignerInfo vào <paramref name="writer"/>.</summary>
        void WriteSignerInfo(AsnWriter writer, byte[] signedAttributes, byte[] chuKyTho,
            X509Certificate2 cert, byte[] tsaToken);

        /// <summary>Ghi AlgorithmIdentifier; RSA cần tham số NULL, SHA-2 thì phải bỏ trống.</summary>
        void WriteAlgorithm(AsnWriter writer, string oid, bool coThamSoNull);
    }
}
