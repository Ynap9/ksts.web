using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kssm.be.domain.DongGoi
{
    public class PackageDossier : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SessionId { get; set; }

        [MaxLength(500)]
        public string FolderName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FileCode { get; set; }

        [MaxLength(200)]
        public string? ExcelObjectKey { get; set; }

        [MaxLength(500)]
        public string? ExcelFileName { get; set; }

        [MaxLength(100)]
        public string? PackageObjid { get; set; }

        [MaxLength(200)]
        public string? PackageObjectKey { get; set; }

        public long PackageSize { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = KiemTraConstants.StatusPending;

        [MaxLength(4000)]
        public string? ErrorSummary { get; set; }

        public int DocumentCount { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
    }
}
