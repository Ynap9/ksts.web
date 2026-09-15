using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kssm.be.domain.DongGoi
{
    public class PackageFieldConfig : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PackageVariantId { get; set; }

        public int MetadataFieldId { get; set; }

        [MaxLength(50)]
        public string EadElement { get; set; } = string.Empty;

        public FieldRequirement Requirement { get; set; }

        [MaxLength(50)]
        public string? ConditionField { get; set; }

        [MaxLength(100)]
        public string? ConditionValues { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(2000)]
        public string? DescriptionOverride { get; set; }

        public MetadataDataType? DataTypeOverride { get; set; }

        public int? FieldLengthOverride { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
    }
}
