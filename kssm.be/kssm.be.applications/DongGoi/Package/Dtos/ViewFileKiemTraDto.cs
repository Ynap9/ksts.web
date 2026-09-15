namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class ViewFileKiemTraDto
    {
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string RelativePath { get; set; } = string.Empty;

        public string? DocId { get; set; }

        public string TrangThai { get; set; } = string.Empty;

        public string? LyDo { get; set; }

        public bool DaKySo { get; set; }

        public string? NguoiKy { get; set; }

        public string? PdfAPart { get; set; }

        public string? PdfAConformance { get; set; }

        public bool HaiLop { get; set; }

        public int SoTrang { get; set; }
    }
}
