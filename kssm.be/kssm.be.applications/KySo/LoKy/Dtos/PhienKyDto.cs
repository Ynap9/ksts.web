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

        /// <summary>Kho chứa bản NGUỒN: kho mặc định cho lô tải lên, MinIO của dự án cho lô dự án.</summary>
        public S3ConnectionDto Kho { get; set; } = new();

        /// <summary>Thư mục trên Google Drive nhận bản đã ký, chốt lúc bắt đầu lô.</summary>
        public string DriveFolderId { get; set; } = string.Empty;
    }
}
