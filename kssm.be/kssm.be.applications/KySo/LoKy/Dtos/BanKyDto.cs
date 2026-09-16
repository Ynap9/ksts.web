using kssm.be.external.KySo.Pdf.Dtos;

namespace kssm.be.applications.KySo.LoKy.Dtos
{
    public class BanKyDto
    {
        public ViecKyDto Viec { get; set; } = new();

        public PdfPreparedDto Prepared { get; set; } = new();

        public byte[] SignedAttributes { get; set; } = Array.Empty<byte>();

        public byte[] ChuKyTho { get; set; } = Array.Empty<byte>();

        public DateTime SignedAt { get; set; }

        public long MsTai { get; set; }

        public long MsDung { get; set; }

        public long MsKy { get; set; }
    }
}
