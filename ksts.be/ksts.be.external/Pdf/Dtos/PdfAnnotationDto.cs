namespace ksts.be.external.Pdf.Dtos
{
    public class PdfAnnotationDto
    {
        public int Number { get; set; }

        public int PageObjectNumber { get; set; }

        public PdfRectPointsDto Rect { get; set; } = new();

        public int? AppearanceObjectNumber { get; set; }

        public bool LaChuKy { get; set; }
    }
}
