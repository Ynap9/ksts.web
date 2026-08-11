namespace ksts.be.external.Pdf.Dtos
{
    public class PdfAnnotationPlanDto
    {
        public List<PdfAnnotationDto> Annotations { get; set; } = new();

        public List<PdfAppearanceObjectDto> AppearanceObjects { get; set; } = new();

        public int NextObjectNumber { get; set; }
    }
}
