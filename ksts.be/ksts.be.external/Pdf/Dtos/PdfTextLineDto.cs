namespace ksts.be.external.Pdf.Dtos
{
    public class PdfTextLineDto
    {
        public string Text { get; set; } = string.Empty;

        public string NormalizedText { get; set; } = string.Empty;

        public double LeftRatio { get; set; }

        public double TopRatio { get; set; }

        public double WidthRatio { get; set; }

        public double HeightRatio { get; set; }

        public double CenterXRatio => LeftRatio + WidthRatio / 2;

        public double CenterYRatio => TopRatio + HeightRatio / 2;
    }
}
