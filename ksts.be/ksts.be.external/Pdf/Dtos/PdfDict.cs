namespace ksts.be.external.Pdf.Dtos
{
    public class PdfDict
    {
        public List<KeyValuePair<string, object?>> Items { get; } = new();

        public PdfDict Add(string key, object? value)
        {
            Items.Add(new KeyValuePair<string, object?>(key, value));
            return this;
        }
    }
}
