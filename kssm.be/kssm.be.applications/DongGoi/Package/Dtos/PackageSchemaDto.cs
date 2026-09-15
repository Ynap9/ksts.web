using kssm.be.shared.Constants.DongGoi;

namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class PackageSchemaDto
    {
        public MetadataObjectType ObjectType { get; set; }

        public string VariantKey { get; set; } = string.Empty;

        public string SheetName { get; set; } = string.Empty;

        public List<SchemaFieldDto> Fields { get; set; } = new();
    }

    public class SchemaFieldDto
    {
        public string FieldKey { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string EadElement { get; set; } = string.Empty;

        public List<string> Aliases { get; set; } = new();

        public FieldRequirement Requirement { get; set; }

        public string? ConditionField { get; set; }

        public string? ConditionValues { get; set; }

        public MetadataDataType DataType { get; set; }

        public int? FieldLength { get; set; }

        public List<string> Codes { get; set; } = new();
    }
}
