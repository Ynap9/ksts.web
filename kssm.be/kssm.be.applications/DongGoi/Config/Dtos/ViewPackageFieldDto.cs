using kssm.be.shared.Constants.DongGoi;

namespace kssm.be.applications.DongGoi.Config.Dtos
{
    public class ViewPackageFieldDto
    {
        public int Id { get; set; }
        public string VariantKey { get; set; } = string.Empty;
        public MetadataObjectType ObjectType { get; set; }
        public string FieldKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string EadElement { get; set; } = string.Empty;
        public FieldRequirement Requirement { get; set; }
        public string? ConditionField { get; set; }
        public string? ConditionValues { get; set; }
        public MetadataDataType DataType { get; set; }
        public int? FieldLength { get; set; }
        public int SortOrder { get; set; }
        public List<ViewCodeValueDto> Codes { get; set; } = new();
    }
}
