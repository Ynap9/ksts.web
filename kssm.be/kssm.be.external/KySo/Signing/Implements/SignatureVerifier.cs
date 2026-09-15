using kssm.be.external.KySo.Pdf.Dtos;
using kssm.be.external.KySo.Signing.Dtos;
using kssm.be.external.KySo.Signing.Interfaces;
using kssm.be.external.KySo.Tsa.Interfaces;
using kssm.be.shared.Constants.Signing;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;

namespace kssm.be.external.KySo.Signing.Implements
{
    public class SignatureVerifier : ISignatureVerifier
    {
        private readonly ITimestampClient _timestampClient;

        public SignatureVerifier(ITimestampClient timestampClient)
        {
            _timestampClient = timestampClient;
        }

        public KiemChuKyDto Verify(PdfSignatureDto chuKy)
        {
            if (!chuKy.PhuTronFile)
            {
                return new KiemChuKyDto
                {
                    LyDo = "/ByteRange không phủ trọn file: có byte nằm ngoài phạm vi chữ ký bảo vệ.",
                };
            }

            SignedCms cms;
            try
            {
                cms = new SignedCms(new ContentInfo(chuKy.SignedContent), detached: true);
                cms.Decode(chuKy.Cms);
            }
            catch (Exception)
            {
                return new KiemChuKyDto { LyDo = "Khối chữ ký trong file không đọc được." };
            }

            if (cms.SignerInfos.Count == 0)
            {
                return new KiemChuKyDto { LyDo = "Khối chữ ký không có người ký nào." };
            }

            try
            {
                // verifySignatureOnly bỏ phần thẩm định chứng thư — đã làm lúc mở phiên. Phép này vẫn đối
                // chiếu messageDigest với nội dung nên bắt được file bị sửa sau khi ký.
                cms.CheckSignature(verifySignatureOnly: true);
            }
            catch (CryptographicException ex)
            {
                return new KiemChuKyDto { LyDo = $"Chữ ký không khớp nội dung file: {ex.Message}" };
            }

            var token = cms.SignerInfos[0].UnsignedAttributes
                .Cast<CryptographicAttributeObject>()
                .FirstOrDefault(x => x.Oid.Value == SignatureConstants.OidSignatureTimeStampToken)
                ?.Values.Cast<AsnEncodedData>().FirstOrDefault()?.RawData;

            if (token == null)
            {
                return new KiemChuKyDto { LyDo = "Chữ ký thiếu dấu thời gian của TSA." };
            }

            return _timestampClient.DocGenTime(token) == null
                ? new KiemChuKyDto { LyDo = "Dấu thời gian của TSA không đọc được." }
                : new KiemChuKyDto { HopLe = true };
        }
    }
}
