using kssm.be.external.Pdf.Dtos;

namespace kssm.be.external.Pdf.Interfaces
{
    /// <summary>
    /// Tells whether a PDF already carries a digital signature, so the batch can refuse to sign it again
    /// unless the template explicitly allows signing over an existing signature.
    /// </summary>
    public interface IPdfSignatureInspector
    {
        /// <summary>Returns true when the file contains at least one filled signature dictionary.</summary>
        bool HasSignature(byte[] bytes);

        /// <summary>
        /// Tách phần nội dung mà /ByteRange phủ và khối CMS trong /Contents, để tầng ký kiểm lại chữ ký trên
        /// chính file vừa ghi ra. Trả null khi file không có chữ ký đã điền.
        /// </summary>
        PdfSignatureDto? ReadSignature(byte[] bytes);
    }
}
