using kssm.be.shared.Constants.LoKy;
using kssm.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kssm.be.domain.KySo.LoKy
{
    /// <summary>
    /// Một lô ký. Lô của dự án chỉ giữ <see cref="ProjectId"/> và tên dự án chụp lại lúc mở lô — cấu hình
    /// MinIO của dự án đọc lại từ MongoDB mỗi lần cần, không chép khoá truy cập xuống SQL.
    /// </summary>
    public class LoKy : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(450)]
        public string? NguoiTaoId { get; set; }

        public int TemplateId { get; set; }

        [MaxLength(50)]
        public string? ProjectId { get; set; }

        [MaxLength(500)]
        public string? TenDuAn { get; set; }

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

        [MaxLength(1000)]
        public string? TienToKho { get; set; }

        public bool DaBaoDuAnKyXong { get; set; }

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
