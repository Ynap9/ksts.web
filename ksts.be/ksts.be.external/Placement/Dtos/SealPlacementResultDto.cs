namespace ksts.be.external.Placement.Dtos
{
    public class SealPlacementResultDto
    {
        public int PageNumber { get; set; }

        public double PageWidthPoints { get; set; }

        public double PageHeightPoints { get; set; }

        public string AnchorChucDanh { get; set; } = string.Empty;

        public string AnchorTenNguoiKy { get; set; } = string.Empty;

        public double MidXRatio { get; set; }

        public double MidYRatio { get; set; }

        public PdfRectRatioDto? DauDo { get; set; }

        public PdfRectRatioDto? ChuKyTuoi { get; set; }

        public string? CanhBao { get; set; }
    }
}
