namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class ViewGoiDto
    {
        public int HoSoId { get; set; }

        public string MaHoSo { get; set; } = string.Empty;

        public string Objid { get; set; } = string.Empty;

        public string TenFile { get; set; } = string.Empty;

        public int SoTaiLieu { get; set; }

        public long DungLuong { get; set; }

        public string Url { get; set; } = string.Empty;
    }
}
