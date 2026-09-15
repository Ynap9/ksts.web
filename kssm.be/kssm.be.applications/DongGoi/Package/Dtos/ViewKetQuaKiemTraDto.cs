namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class ViewKetQuaKiemTraDto
    {
        public ViewPhienDto Phien { get; set; } = new();

        public HeaderMatchReportDto? BaoCaoHoSo { get; set; }

        public List<HeaderMatchReportDto> BaoCaoTaiLieu { get; set; } = new();

        public List<ViewHoSoKiemTraDto> HoSo { get; set; } = new();
    }
}
