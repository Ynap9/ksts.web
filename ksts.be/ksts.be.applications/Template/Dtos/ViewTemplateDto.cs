namespace ksts.be.applications.Template.Dtos
{
    public class ViewTemplateDto
    {
        public int Id { get; set; }

        public string IdUser { get; set; } = string.Empty;

        public string TenTemplate { get; set; } = string.Empty;

        public string Thumbprint { get; set; } = string.Empty;

        public string? TenChungThu { get; set; }

        public string? LyDoKy { get; set; }

        public string? NoiKy { get; set; }

        public string? AnhDauDoUrl { get; set; }

        public string? AnhChuKyTuoiUrl { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool HienThiChuKySo { get; set; }

        public bool NhoiChuKySoVaoAnh { get; set; }

        public int DoDamDauDo { get; set; }

        public int DoDamChuKyTuoi { get; set; }

        public int DoDayNetChuKyTuoi { get; set; }

        public List<TemplatePositionDto> Positions { get; set; } = new();
    }
}
