namespace kssm.be.external.DongGoi.Dtos
{
    public class PackFileDto
    {
        public string FileName { get; set; } = string.Empty;

        public string FileId { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public long Size { get; set; }

        public string Checksum { get; set; } = string.Empty;
    }
}
