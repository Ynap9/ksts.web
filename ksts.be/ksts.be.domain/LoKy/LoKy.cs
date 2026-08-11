using ksts.be.shared.Constants.LoKy;
using Sip.be.Shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ksts.be.domain.LoKy
{
    public class LoKy : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(450)]
        public string IdUser { get; set; } = string.Empty;

        public int TemplateId { get; set; }

        [MaxLength(100)]
        public string? Thumbprint { get; set; }

        [MaxLength(100)]
        public string TaiToken { get; set; } = string.Empty;

        public TrangThaiLoKy TrangThai { get; set; }

        public int TongSo { get; set; }

        public int DaXong { get; set; }

        public int SoLoi { get; set; }

        [MaxLength(1000)]
        public string? LoiChung { get; set; }

        public DateTime? ThoiDiemBatDau { get; set; }

        public DateTime? ThoiDiemXong { get; set; }

        public bool DangDayLenKho { get; set; }

        public int DaDayLenKho { get; set; }

        public int SoLoiDayLenKho { get; set; }

        public bool HoanTatDayLenKho { get; set; }

        [MaxLength(1000)]
        public string? LoiDayLenKho { get; set; }

        [MaxLength(1000)]
        public string? TienToKho { get; set; }

        public List<LoKyFile> Files { get; set; } = new();

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
