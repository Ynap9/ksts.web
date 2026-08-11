using ksts.be.applications.Signing.Dtos;
using ksts.be.external.Certificates.Dtos;

namespace ksts.be.applications.Signing.Interfaces
{
    /// <summary>
    /// Lấy và chọn chứng thư số để ký.
    ///
    /// Nguồn chứng thư do <see cref="external.Certificates.Interfaces.ICertificateProvider"/> quyết định; giai
    /// đoạn này nó đọc cert store của MÁY CHẠY API. Xem .claude/docs/ky-so-web-vs-desktop.md về giới hạn đó và
    /// hướng chuyển sang agent ở máy client.
    /// </summary>
    public interface ICertificateService
    {
        /// <summary>
        /// Danh sách chứng thư số. Mặc định trả HẾT kèm cờ CanSign và Reason để người dùng biết vì sao một
        /// cert không chọn được; <paramref name="query"/>.OnlySignable = true thì chỉ trả cert ký được.
        /// </summary>
        List<SignCertDto> GetCertificates(SignCertQueryDto query);

        /// <summary>
        /// Thông tin chẩn đoán việc quét kho chứng thư: store nào mở được, đọc ra bao nhiêu cert.
        /// Trên máy người dùng không có debugger, đây là đường duy nhất để biết vì sao danh sách rỗng.
        /// </summary>
        CertDiagnosticDto GetDiagnostics();

        /// <summary>
        /// Chọn một chứng thư theo thumbprint: thẩm định LẠI ngay tại thời điểm chọn rồi trả về chi tiết.
        /// Thẩm định lại chứ không tin danh sách đã lấy trước đó - token có thể vừa bị rút, chứng thư có thể
        /// vừa hết hạn giữa hai lời gọi.
        /// Không tìm thấy thì ném CertificateNotFound; không đủ điều kiện thì ném CertificateCannotSign.
        /// </summary>
        SignCertDto SelectCertificate(SelectCertDto input);
    }
}
