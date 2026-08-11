namespace ksts.be.applications.LoKy.Dtos
{
    public class ViewLoKyDto
    {
        public int Id { get; set; }

        public int TemplateId { get; set; }

        public string? Thumbprint { get; set; }

        public string TaiToken { get; set; } = string.Empty;

        public string TrangThai { get; set; } = string.Empty;

        public int TongSo { get; set; }

        public int DaXong { get; set; }

        public int SoLoi { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
}
