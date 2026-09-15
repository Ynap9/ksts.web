namespace kssm.be.external.KySo.Pdf.Dtos
{
    public class PdfAppearanceDto
    {
        public int FormObjectNumber { get; set; }

        public List<PdfAppearanceObjectDto> Objects { get; set; } = new();
    }
}
