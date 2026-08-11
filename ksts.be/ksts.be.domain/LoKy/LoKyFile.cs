using ksts.be.shared.Constants.LoKy;
using Sip.be.Shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ksts.be.domain.LoKy
{
    public class LoKyFile : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int LoKyId { get; set; }

        public LoKy? LoKy { get; set; }

        public int ThuTu { get; set; }

        [MaxLength(500)]
        public string TenFile { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string ObjectKeyNguon { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ObjectKeyDaKy { get; set; }

        public TrangThaiFileKy TrangThai { get; set; }

        [MaxLength(1000)]
        public string? LyDoLoi { get; set; }

        public DateTime? ThoiGianKy { get; set; }

        public DateTime? DauThoiGian { get; set; }

        [MaxLength(450)]
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        [MaxLength(450)]
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool Deleted { get; set; }
        [MaxLength(450)]
        public string? DeletedBy { get; set; }
    }
}
