namespace kssm.be.applications.DongGoi.Config.Dtos
{
    public class ExportFileDto
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
    }
}
