namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class ViewTienDoKiemTraDto
    {
        public int PhienId { get; set; }

        public string TrangThai { get; set; } = string.Empty;

        public bool DangChay { get; set; }

        public bool HoanTat { get; set; }

        public int TongSo { get; set; }

        public int DaXong { get; set; }

        public int SoLoi { get; set; }

        public int SoCanhBao { get; set; }

        public string? LyDoDung { get; set; }

        public List<ViewFileKiemTraDto> FilesVuaXong { get; set; } = new();

        public List<ViewFileKiemTraDto> FilesLoi { get; set; } = new();
    }
}
