using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kssm.be.domain.DongGoi
{
    public class MetadataField : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(50)]
        public string FieldKey { get; set; } = string.Empty;

        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public MetadataDataType DataType { get; set; }

        public int? FieldLength { get; set; }

        [MaxLength(50)]
        public string? CodeGroup { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
    }
}
