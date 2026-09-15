namespace kssm.be.external.DongGoi.Dtos
{
    public class PdfInspectResultDto
    {
        public PdfFormatVerdict Verdict { get; set; }

        public string? PdfAPart { get; set; }

        public string? PdfAConformance { get; set; }

        public bool HasImageLayer { get; set; }

        public bool HasTextLayer { get; set; }

        public int PageCount { get; set; }

        public string? Reason { get; set; }
    }
}
