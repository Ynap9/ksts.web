using kssm.be.external.KySo.Pdf.Dtos;
using kssm.be.external.KySo.Signing.Dtos;

namespace kssm.be.external.KySo.Signing.Interfaces
{
    /// <summary>
    /// Kiểm lại chữ ký trên chính file vừa ghi ra: nội dung không đổi so với lúc ký, chữ ký khớp khoá của
    /// chứng thư, và có dấu thời gian TSA đọc được.
    ///
    /// KHÔNG dựng chuỗi CA: cả lô ký bằng một chứng thư và chuỗi đó đã thẩm định một lần lúc mở phiên, dựng
    /// lại cho từng file chỉ tốn thêm thời gian mà không biết thêm điều gì.
    /// </summary>
    public interface ISignatureVerifier
    {
        KiemChuKyDto Verify(PdfSignatureDto chuKy);
    }
}
