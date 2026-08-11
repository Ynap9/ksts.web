namespace ksts.be.external.Pdf.Dtos
{
    public class PdfAppearanceDto
    {
        public int FormObjectNumber { get; set; }

        public List<PdfAppearanceObjectDto> Objects { get; set; } = new();
    }
}
