namespace ksts.be.external.Pdf.Dtos
{
    public class PdfAppearanceObjectDto
    {
        public int Number { get; set; }

        public string DictText { get; set; } = string.Empty;

        public byte[]? StreamData { get; set; }
    }
}
