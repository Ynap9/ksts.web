namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class ViewDongGoiDto
    {
        public int PhienId { get; set; }

        public int SoGoi { get; set; }

        public int SoTaiLieu { get; set; }

        public List<ViewGoiDto> Goi { get; set; } = new();

        public List<string> BoQua { get; set; } = new();
    }
}
