using kssm.be.external.KySo.Pdf.Dtos;
using kssm.be.external.KySo.Pdf.Interfaces;
using kssm.be.shared.Constants.Signing;
using System.Formats.Asn1;
using System.Text;
using System.Text.RegularExpressions;

namespace kssm.be.external.KySo.Pdf.Implements
{
    public class PdfSignatureInspector : IPdfSignatureInspector
    {
        public bool HasSignature(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return false;
            }

            // Latin1 keeps one byte per char, so the scan never splits a marker in half. A signature
            // dictionary may never live inside an object stream (PDF 32000 §12.8.1), so plain text search
            // over the raw file is enough — no need to walk the xref chain just to answer yes or no.
            var text = Encoding.Latin1.GetString(bytes);
            return Regex.IsMatch(text, SigningConstants.SignatureValueMarker);
        }

        public PdfSignatureDto? ReadSignature(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return null;
            }

            var text = Encoding.Latin1.GetString(bytes);

            // Ký đè nối thêm một bản sửa đổi vào cuối file, nên chữ ký MỚI NHẤT là cái nằm sau cùng.
            var byteRange = Regex.Matches(text, SigningConstants.ByteRangePattern).LastOrDefault();
            var contents = Regex.Matches(text, SigningConstants.ContentsPattern).LastOrDefault();

            if (byteRange == null || contents == null)
            {
                return null;
            }

            var dauOffset = int.Parse(byteRange.Groups[1].Value);
            var dauDai = int.Parse(byteRange.Groups[2].Value);
            var sauOffset = int.Parse(byteRange.Groups[3].Value);
            var sauDai = int.Parse(byteRange.Groups[4].Value);

            if (dauOffset < 0 || dauDai < 0 || sauDai < 0 || sauOffset < dauOffset + dauDai
                || sauOffset + sauDai > bytes.Length)
            {
                return null;
            }

            var noiDung = new byte[dauDai + sauDai];
            Array.Copy(bytes, dauOffset, noiDung, 0, dauDai);
            Array.Copy(bytes, sauOffset, noiDung, dauDai, sauDai);

            // Latin1 nên vị trí ký tự trùng vị trí byte; hex nằm giữa '<' và '>' của /Contents.
            var hex = contents.Groups[1];

            return new PdfSignatureDto
            {
                SignedContent = noiDung,
                Cms = CatKhoiDer(hex.Value),
                // PAdES (ETSI EN 319 142) đòi /ByteRange phủ TRỌN file, chỉ chừa đúng lỗ /Contents. Bỏ phép
                // kiểm này thì byte nối thêm vào cuối file vẫn qua được chữ ký — đó là họ shadow attack.
                PhuTronFile = dauOffset == 0
                    && dauDai == hex.Index - 1
                    && sauOffset == hex.Index + hex.Length + 1
                    && sauOffset + sauDai == bytes.Length,
            };
        }

        /// <summary>
        /// Bỏ phần '0' đệm sau khối DER. Chuẩn cho phép đệm cho đủ chỗ đã chừa, nhưng bộ giải mã ASN.1 coi
        /// mấy byte thừa đó là rác và từ chối đọc.
        /// </summary>
        public byte[] CatKhoiDer(string hex)
        {
            var raw = Convert.FromHexString(hex.Length % 2 == 0 ? hex : hex[..^1]);

            try
            {
                AsnDecoder.ReadEncodedValue(raw, AsnEncodingRules.DER, out _, out _, out var doDai);
                return raw[..doDai];
            }
            catch (AsnContentException)
            {
                return raw;
            }
        }
    }
}
