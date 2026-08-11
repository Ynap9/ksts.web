namespace ksts.be.external.Pdf.Dtos
{
    public class PdfPageTextDto
    {
        public int PageNumber { get; set; }

        public int PageCount { get; set; }

        public double WidthPoints { get; set; }

        public double HeightPoints { get; set; }

        public List<PdfTextLineDto> Lines { get; set; } = new();
    }
}
