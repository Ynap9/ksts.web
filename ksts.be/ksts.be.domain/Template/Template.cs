using ksts.be.shared.Constants.Template;
using Sip.be.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ksts.be.domain.Template
{
    public class Template : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(450)]
        public string IdUser { get; set; } = string.Empty;

        [MaxLength(250)]
        public string TenTemplate { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Thumbprint { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? TenChungThu { get; set; }

        [MaxLength(500)]
        public string? LyDoKy { get; set; }

        [MaxLength(500)]
        public string? NoiKy { get; set; }

        [MaxLength(1000)]
        public string? AnhDauDoUrl { get; set; }

        [MaxLength(1000)]
        public string? AnhDauDoObjectKey { get; set; }

        [MaxLength(1000)]
        public string? AnhChuKyTuoiUrl { get; set; }

        [MaxLength(1000)]
        public string? AnhChuKyTuoiObjectKey { get; set; }

        public bool HienThiChuKySo { get; set; } = true;

        public bool NhoiChuKySoVaoAnh { get; set; }

        public bool KyDe { get; set; }

        public int DoDamDauDo { get; set; } = TemplateConstants.DoDamMacDinh;

        public int DoDamChuKyTuoi { get; set; } = TemplateConstants.DoDamMacDinh;

        public int DoDayNetChuKyTuoi { get; set; } = TemplateConstants.DoDayNetMacDinh;

        [MaxLength(7)]
        public string MauChuKySo { get; set; } = TemplateConstants.MauMacDinh;

        [MaxLength(7)]
        public string? MauChuKyTuoi { get; set; }

        public List<TemplatePosition> Positions { get; set; } = new();

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
