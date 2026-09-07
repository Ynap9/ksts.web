namespace kssm.be.applications.LoKy.Dtos
{
    public class NopChuKyDto
    {
        public string YeuCauId { get; set; } = string.Empty;

        public string? ChuKyBase64 { get; set; }

        public string? Loi { get; set; }

        public bool PhienDaMat { get; set; }
    }
}
