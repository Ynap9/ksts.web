namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class ViewHoSoKiemTraDto
    {
        public int Id { get; set; }

        public string TenThuMuc { get; set; } = string.Empty;

        public string? MaHoSo { get; set; }

        public string TrangThai { get; set; } = string.Empty;

        public List<string> Loi { get; set; } = new();

        public int SoTaiLieu { get; set; }

        public List<ViewFileKiemTraDto> Files { get; set; } = new();
    }
}
