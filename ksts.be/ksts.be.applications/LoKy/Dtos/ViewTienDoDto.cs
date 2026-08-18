namespace ksts.be.applications.LoKy.Dtos
{
    public class ViewTienDoDto
    {
        public int Id { get; set; }

        public string TrangThai { get; set; } = string.Empty;

        public string TaiToken { get; set; } = string.Empty;

        public int TongSo { get; set; }

        public int DaXong { get; set; }

        public int SoLoi { get; set; }

        public bool DangChay { get; set; }

        public bool HoanTat { get; set; }

        public bool CoTheTaiZip { get; set; }

        public string? LoiChung { get; set; }

        public string? TienToKho { get; set; }

        public List<ViewFileKyDto> FilesLoi { get; set; } = new();

        public List<ViewFileKyDto> FilesVuaXong { get; set; } = new();
    }
}
