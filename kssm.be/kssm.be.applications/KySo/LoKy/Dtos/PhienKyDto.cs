using kssm.be.external.KySo.Pdf.Dtos;
using kssm.be.external.S3.Dtos;
using System.Security.Cryptography.X509Certificates;

namespace kssm.be.applications.KySo.LoKy.Dtos
{
    public class PhienKyDto
    {
        public int LoKyId { get; set; }

        public string? ProjectId { get; set; }

        public X509Certificate2 Cert { get; set; } = null!;

        public IReadOnlyList<X509Certificate2> ChuoiChungThu { get; set; } = Array.Empty<X509Certificate2>();

        public PdfPrepareOptionsDto TuyChonMau { get; set; } = new();

        public bool KyDe { get; set; }

        /// <summary>Kho chứa bản nguồn và cũng là nơi ghi bản đã ký: kho mặc định, hoặc MinIO của dự án.</summary>
        public S3ConnectionDto Kho { get; set; } = new();

        /// <summary>Tiền tố thư mục nhận bản đã ký, chốt lúc bắt đầu lô để cả lô ghi về cùng một chỗ.</summary>
        public string TienToDaKy { get; set; } = string.Empty;
    }
}
