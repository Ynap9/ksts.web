namespace ksts.be.external.Jobs.Dtos
{
    public class ZipJobDto
    {
        public string JobId { get; set; } = string.Empty;

        public string TaiToken { get; set; } = string.Empty;

        public int TongSo { get; set; }

        public int DaXong { get; set; }

        public int SoLoi { get; set; }

        public bool HoanTat { get; set; }

        public string? LoiChung { get; set; }

        public long DungLuong { get; set; }

        public DateTime HetHanUtc { get; set; }

        public string? TienToKho { get; set; }

        public List<string> TenFileDaDay { get; set; } = [];
    }
}
