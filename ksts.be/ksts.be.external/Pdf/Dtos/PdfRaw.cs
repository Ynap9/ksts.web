namespace ksts.be.external.Pdf.Dtos
{
    public class PdfRaw
    {
        public PdfRaw(string text) => Text = text;

        public string Text { get; }
    }
}
