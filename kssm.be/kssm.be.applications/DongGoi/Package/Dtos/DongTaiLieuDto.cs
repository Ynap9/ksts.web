namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class DongTaiLieuDto
    {
        public SheetTaiLieuDto Sheet { get; set; } = new();

        public Dictionary<string, string>? Dong { get; set; }
    }
}
