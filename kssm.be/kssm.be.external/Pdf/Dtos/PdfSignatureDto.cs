namespace kssm.be.external.Pdf.Dtos
{
    public class PdfSignatureDto
    {
        public byte[] SignedContent { get; set; } = [];

        public byte[] Cms { get; set; } = [];

        public bool PhuTronFile { get; set; }
    }
}
