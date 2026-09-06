namespace kssm.be.applications.Plugin.Dtos
{
    public class ViewPhienBanPluginDto
    {
        public string PhienBan { get; set; } = string.Empty;

        public bool PhuHop { get; set; }

        public List<string> DanhSachPhuHop { get; set; } = [];

        public string? LyDo { get; set; }
    }
}
