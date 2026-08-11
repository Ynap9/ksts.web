using ksts.be.shared.Constants.Template;
using Microsoft.AspNetCore.Http;

namespace ksts.be.applications.Template.Dtos
{
    public class AddConfigTemplateDto
    {
        public int Id { get; set; }

        public string Thumbprint { get; set; } = string.Empty;

        public string? TenChungThu { get; set; }

        public string? LyDoKy { get; set; }

        public string? NoiKy { get; set; }

        public bool HienThiChuKySo { get; set; } = true;

        public bool NhoiChuKySoVaoAnh { get; set; }

        public int DoDamDauDo { get; set; } = TemplateConstants.DoDamMacDinh;

        public int DoDamChuKyTuoi { get; set; } = TemplateConstants.DoDamMacDinh;

        public int DoDayNetChuKyTuoi { get; set; } = TemplateConstants.DoDayNetMacDinh;

        public IFormFile? AnhDauDo { get; set; }

        public IFormFile? AnhChuKyTuoi { get; set; }

        public List<TemplatePositionDto> Positions { get; set; } = new();
    }
}
