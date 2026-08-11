using ksts.be.external.Signing.Interfaces;
using ksts.be.shared.Constants.Signing;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Signing.Implements
{
    /// <summary>
    /// Dựng CMS SignedData detached bằng AsnWriter theo RFC 5652.
    /// </summary>
    public class CmsAssembler : ICmsAssembler
    {
        /// <summary>Thẻ SET OF của DER. Đổi sang thẻ [0] IMPLICIT khi nhét SignedAttributes vào SignerInfo.</summary>
        private const byte TagSetOf = 0x31;
        private const byte TagContext0 = 0xA0;

        /// <inheritdoc/>
        public byte[] BuildSignedAttributes(byte[] noiDungHash, X509Certificate2 cert, DateTime signedAtUtc)
        {
            var writer = new AsnWriter(AsnEncodingRules.DER);

            // PopSetOf của DER tự sắp xếp các phần tử, nên thứ tự viết ở đây không ảnh hưởng kết quả.
            writer.PushSetOf();

            writer.PushSequence();
            writer.WriteObjectIdentifier(SigningConstants.OidContentType);
            writer.PushSetOf();
            writer.WriteObjectIdentifier(SigningConstants.OidData);
            writer.PopSetOf();
            writer.PopSequence();

            writer.PushSequence();
            writer.WriteObjectIdentifier(SigningConstants.OidMessageDigest);
            writer.PushSetOf();
            writer.WriteOctetString(noiDungHash);
            writer.PopSetOf();
            writer.PopSequence();

            writer.PushSequence();
            writer.WriteObjectIdentifier(SignatureConstants.OidSigningTime);
            writer.PushSetOf();
            writer.WriteUtcTime(new DateTimeOffset(signedAtUtc, TimeSpan.Zero));
            writer.PopSetOf();
            writer.PopSequence();

            writer.PushSequence();
            writer.WriteObjectIdentifier(SigningConstants.OidSigningCertificateV2);
            writer.PushSetOf();
            writer.WriteEncodedValue(BuildSigningCertificateV2(cert));
            writer.PopSetOf();
            writer.PopSequence();

            writer.PopSetOf();
            return writer.Encode();
        }

        /// <inheritdoc/>
        public byte[] BuildSigningCertificateV2(X509Certificate2 cert)
        {
            // SigningCertificateV2 ::= SEQUENCE { certs SEQUENCE OF ESSCertIDv2 }
            // ESSCertIDv2 ::= SEQUENCE { hashAlgorithm DEFAULT sha256, certHash OCTET STRING }
            // Bỏ hashAlgorithm vì đang dùng đúng giá trị mặc định; DER cấm ghi lại giá trị DEFAULT.
            var writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.PushSequence();
            writer.PushSequence();
            writer.WriteOctetString(SHA256.HashData(cert.RawData));
            writer.PopSequence();
            writer.PopSequence();
            writer.PopSequence();
            return writer.Encode();
        }

        /// <inheritdoc/>
        public byte[] Assemble(byte[] signedAttributes, byte[] chuKyTho, X509Certificate2 cert,
            IReadOnlyList<X509Certificate2> chuoiChungThu, byte[] tsaToken)
        {
            if (signedAttributes.Length == 0 || signedAttributes[0] != TagSetOf)
            {
                throw new UserFriendlyException(ErrorCodes.SignatureAssembleFailed,
                    "SignedAttributes không đúng dạng SET OF nên không ghép được chữ ký.");
            }

            var writer = new AsnWriter(AsnEncodingRules.DER);

            writer.PushSequence();
            writer.WriteObjectIdentifier(SigningConstants.OidSignedData);
            writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

            writer.PushSequence();
            writer.WriteInteger(1);

            writer.PushSetOf();
            WriteAlgorithm(writer, SigningConstants.OidSha256, false);
            writer.PopSetOf();

            // Detached: khai kiểu nội dung nhưng KHÔNG nhúng nội dung, vì nội dung là chính file PDF.
            writer.PushSequence();
            writer.WriteObjectIdentifier(SigningConstants.OidData);
            writer.PopSequence();

            writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
            foreach (var item in chuoiChungThu)
            {
                writer.WriteEncodedValue(item.RawData);
            }
            writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));

            writer.PushSetOf();
            WriteSignerInfo(writer, signedAttributes, chuKyTho, cert, tsaToken);
            writer.PopSetOf();

            writer.PopSequence();
            writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            writer.PopSequence();

            return writer.Encode();
        }

        /// <summary>Ghi một SignerInfo. Tách riêng để phần SignedData ở trên đọc được thành một mạch.</summary>
        public void WriteSignerInfo(AsnWriter writer, byte[] signedAttributes, byte[] chuKyTho,
            X509Certificate2 cert, byte[] tsaToken)
        {
            writer.PushSequence();
            writer.WriteInteger(1);

            // SignerIdentifier: issuerAndSerialNumber. Lấy thẳng DER của issuer và serial trong chứng thư,
            // dựng lại tay là nguồn của sai lệch một byte làm chữ ký không khớp.
            writer.PushSequence();
            writer.WriteEncodedValue(cert.IssuerName.RawData);
            writer.WriteInteger(cert.SerialNumberBytes.Span);
            writer.PopSequence();

            WriteAlgorithm(writer, SigningConstants.OidSha256, false);

            // Cùng một chuỗi byte plugin đã ký, chỉ đổi thẻ SET OF sang [0] IMPLICIT. Dựng lại bộ thuộc tính
            // lần hai là rủi ro thật: lệch một byte thì chữ ký hợp lệ vẫn bị coi là sai.
            var implicitAttrs = signedAttributes.ToArray();
            implicitAttrs[0] = TagContext0;
            writer.WriteEncodedValue(implicitAttrs);

            WriteAlgorithm(writer, SigningConstants.OidRsaEncryption, true);
            writer.WriteOctetString(chuKyTho);

            writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
            writer.PushSequence();
            writer.WriteObjectIdentifier(SignatureConstants.OidSignatureTimeStampToken);
            writer.PushSetOf();
            writer.WriteEncodedValue(tsaToken);
            writer.PopSetOf();
            writer.PopSequence();
            writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));

            writer.PopSequence();
        }

        /// <summary>
        /// AlgorithmIdentifier. RSA đòi tham số NULL tường minh (RFC 3370); SHA-2 thì phải bỏ trống
        /// (RFC 5754) — ghi NULL vào đó là sai chuẩn.
        /// </summary>
        public void WriteAlgorithm(AsnWriter writer, string oid, bool coThamSoNull)
        {
            writer.PushSequence();
            writer.WriteObjectIdentifier(oid);
            if (coThamSoNull)
            {
                writer.WriteNull();
            }
            writer.PopSequence();
        }
    }
}
