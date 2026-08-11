namespace ksts.be.external.Pdf.Dtos
{
    public class PdfRevisionDto
    {
        public byte[] Bytes { get; set; } = Array.Empty<byte>();

        public string Text { get; set; } = string.Empty;

        public long LastXrefOffset { get; set; }

        public bool LastXrefIsStream { get; set; }

        public int Size { get; set; }

        public int RootObjectNumber { get; set; }

        public string? IdRaw { get; set; }

        public string? InfoRaw { get; set; }

        public Dictionary<int, PdfObjectLocationDto> Objects { get; set; } = new();
    }
}
