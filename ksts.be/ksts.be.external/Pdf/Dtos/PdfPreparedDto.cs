namespace ksts.be.external.Pdf.Dtos
{
    public class PdfPreparedDto
    {
        public byte[] Bytes { get; set; } = Array.Empty<byte>();

        public long[] ByteRange { get; set; } = Array.Empty<long>();

        public byte[] NoiDungKy { get; set; } = Array.Empty<byte>();

        public int ContentsHexStart { get; set; }

        public int ContentsHexLength { get; set; }
    }
}
