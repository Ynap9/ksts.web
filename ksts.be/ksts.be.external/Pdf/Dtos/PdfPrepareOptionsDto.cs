using ksts.be.shared.Constants.Template;

namespace ksts.be.external.Pdf.Dtos
{
    public class PdfPrepareOptionsDto
    {
        public string TenNguoiKy { get; set; } = string.Empty;

        public string? LyDoKy { get; set; }

        public string? NoiKy { get; set; }

        public DateTime SignedAt { get; set; }

        public bool HienThiChuKySo { get; set; }

        public bool NhoiChuKySoVaoAnh { get; set; }

        public byte[]? AnhChuKyTuoi { get; set; }

        public int DoDamChuKyTuoi { get; set; } = TemplateConstants.DoDamMacDinh;

        public int DoDayNetChuKyTuoi { get; set; } = TemplateConstants.DoDayNetMacDinh;

        public List<PdfPlacementDto> ViTri { get; set; } = new();
    }
}
