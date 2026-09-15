using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kssm.be.domain.DongGoi
{
    public class PackageDocument : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SessionId { get; set; }

        public int DossierId { get; set; }

        [MaxLength(1000)]
        public string RelativePath { get; set; } = string.Empty;

        [MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ObjectKey { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        [MaxLength(100)]
        public string? DocId { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = KiemTraConstants.StatusPending;

        [MaxLength(2000)]
        public string? Reason { get; set; }

        public bool IsSigned { get; set; }

        [MaxLength(500)]
        public string? SignerName { get; set; }

        public DateTime? SignedAt { get; set; }

        [MaxLength(10)]
        public string? PdfAPart { get; set; }

        [MaxLength(10)]
        public string? PdfAConformance { get; set; }

        public bool TwoLayer { get; set; }

        public int PageCount { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
    }
}
