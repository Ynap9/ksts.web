namespace kssm.be.applications.KySo.LoKy.Dtos
{
    public class ViewLoKyDto
    {
        public int Id { get; set; }

        public int TemplateId { get; set; }

        public string? ProjectId { get; set; }

        public string? TenDuAn { get; set; }

        public string? Thumbprint { get; set; }

        public string TaiToken { get; set; } = string.Empty;

        public string TrangThai { get; set; } = string.Empty;

        public int TongSo { get; set; }

        public int DaXong { get; set; }

        public int SoLoi { get; set; }

        public string? TienToKho { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
}
