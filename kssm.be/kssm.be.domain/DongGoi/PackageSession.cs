using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kssm.be.domain.DongGoi
{
    public class PackageSession : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public PackageType PackageType { get; set; }

        public MetadataObjectType ObjectType { get; set; }

        [MaxLength(50)]
        public string DocumentTypes { get; set; } = string.Empty;

        [MaxLength(500)]
        public string RootFolderName { get; set; } = string.Empty;

        public bool Multi { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = KiemTraConstants.StatusPending;

        public int NextSequence { get; set; }

        public int TotalDocument { get; set; }

        public int DoneDocument { get; set; }

        public int ErrorDocument { get; set; }

        public int WarningDocument { get; set; }

        public DateTime? StartedDate { get; set; }

        public DateTime? FinishedDate { get; set; }

        [MaxLength(2000)]
        public string? FailReason { get; set; }

        public string? MatchReport { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
    }
}
