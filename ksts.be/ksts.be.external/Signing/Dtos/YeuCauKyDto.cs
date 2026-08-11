namespace ksts.be.external.Signing.Dtos
{
    public class YeuCauKyDto
    {
        public string YeuCauId { get; set; } = string.Empty;

        public string DuLieuBase64 { get; set; } = string.Empty;
    }

    public class KetQuaKyDto
    {
        public string YeuCauId { get; set; } = string.Empty;

        public string? ChuKyBase64 { get; set; }

        public string? Loi { get; set; }
    }
}
