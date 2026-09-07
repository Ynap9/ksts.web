using Amazon.S3;
using kssm.be.external.S3.Dtos;

namespace kssm.be.external.S3.Interfaces
{
    /// <summary>
    /// Phát client S3 theo từng cấu hình kho. Mỗi dự án bên sao_mai có MinIO riêng (endpoint, bucket, khoá
    /// truy cập khác nhau) nên một client dựng sẵn lúc khởi động không phục vụ được luồng ký theo dự án.
    /// </summary>
    public interface IS3ClientFactory
    {
        /// <summary>
        /// Client cho một cấu hình kho, dùng lại bản đã dựng cho cùng bộ (endpoint · bucket · access key).
        /// Client giữ pool kết nối nên dựng mới mỗi lần gọi là phí kết nối trong suốt cả lô.
        /// </summary>
        IAmazonS3 Get(S3ConnectionDto connection);

        /// <summary>Cấu hình kho mặc định của service, khai trong appsettings — dùng cho lô file tải lên.</summary>
        S3ConnectionDto DefaultConnection();
    }
}
