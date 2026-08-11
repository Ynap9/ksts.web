using ksts.be.shared.Constants.Template;

namespace ksts.be.external.Pdf.Dtos
{
    public class PdfPlacementDto
    {
        public TemplatePositionKind Kind { get; set; }

        public int PageNumber { get; set; } = 1;

        public double XRatio { get; set; }

        public double YRatio { get; set; }

        public double WidthRatio { get; set; }

        public double HeightRatio { get; set; }
    }
}
