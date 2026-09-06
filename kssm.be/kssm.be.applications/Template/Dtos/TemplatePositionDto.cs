using kssm.be.shared.Constants.Template;

namespace kssm.be.applications.Template.Dtos
{
    public class TemplatePositionDto
    {
        public TemplatePositionKindConstants Kind { get; set; }

        public int PageNumber { get; set; } = 1;

        public double XRatio { get; set; }

        public double YRatio { get; set; }

        public double WidthRatio { get; set; }

        public double HeightRatio { get; set; }
    }
}
