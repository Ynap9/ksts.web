namespace ksts.be.applications.LoKy.Dtos
{
    public class ViewFileKyDto
    {
        public int Id { get; set; }

        public int ThuTu { get; set; }

        public string TenFile { get; set; } = string.Empty;

        public string TrangThai { get; set; } = string.Empty;

        public string? LyDoLoi { get; set; }

        public DateTime? ThoiGianKy { get; set; }

        public DateTime? DauThoiGian { get; set; }
    }
}
