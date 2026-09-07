using kssm.be.external.Pdf.Dtos;
using kssm.be.external.Pdf.Interfaces;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using System.Text;

namespace kssm.be.external.Pdf.Implements
{
    public class PdfContentWriter : IPdfContentWriter
    {
        public byte[] Write(PdfPreparedDto prepared, byte[] cms)
        {
            if (cms.Length == 0)
            {
                throw new UserFriendlyException(ErrorCodes.SignatureAssembleFailed,
                    "Khối chữ ký rỗng nên không ghi vào file được.");
            }

            var hex = Convert.ToHexString(cms);
            if (hex.Length > prepared.ContentsHexLength)
            {
                throw new UserFriendlyException(ErrorCodes.SignatureTooLarge,
                    $"Khối chữ ký {cms.Length} byte vượt chỗ trống {prepared.ContentsHexLength / 2} byte đã chừa.");
            }

            // Chép ra bản mới thay vì ghi đè tại chỗ: bản đã prepare còn phải dùng lại nguyên vẹn nếu lượt ký
            // này hỏng và cả lô chạy tiếp từ file dở.
            var output = prepared.Bytes.ToArray();

            // Phần dư sau khối DER giữ nguyên các số '0' đang đệm sẵn — chuẩn cho phép đệm, và giữ nguyên độ
            // dài là điều kiện để /ByteRange đã vá vẫn đúng.
            Encoding.Latin1.GetBytes(hex).CopyTo(output, prepared.ContentsHexStart);
            return output;
        }
    }
}
