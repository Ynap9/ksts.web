using kssm.be.shared.Constants.DongGoi;

namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class HeaderMatchReportDto
    {
        public MetadataObjectType ObjectType { get; set; }

        public string SheetName { get; set; } = string.Empty;

        public string VariantKey { get; set; } = string.Empty;

        public bool Passed { get; set; }

        public List<FieldMatchDto> Matched { get; set; } = new();

        public List<FieldMatchDto> Missing { get; set; } = new();

        public List<string> ExtraHeaders { get; set; } = new();
    }

    public class FieldMatchDto
    {
        public string FieldKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string? MatchedHeader { get; set; }

        public double Score { get; set; }

        public FieldRequirement Requirement { get; set; }
    }
}
