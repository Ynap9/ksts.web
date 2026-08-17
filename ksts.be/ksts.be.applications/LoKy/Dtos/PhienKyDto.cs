using ksts.be.external.Pdf.Dtos;
using System.Security.Cryptography.X509Certificates;

namespace ksts.be.applications.LoKy.Dtos
{
    public class PhienKyDto
    {
        public int LoKyId { get; set; }

        public X509Certificate2 Cert { get; set; } = null!;

        public IReadOnlyList<X509Certificate2> ChuoiChungThu { get; set; } = Array.Empty<X509Certificate2>();

        public PdfPrepareOptionsDto TuyChonMau { get; set; } = new();

        public bool KyDe { get; set; }
    }
}
