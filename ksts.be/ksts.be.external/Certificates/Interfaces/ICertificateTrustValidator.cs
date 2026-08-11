using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Certificates.Interfaces
{
    /// <summary>
    /// Kiểm chứng thư số có thuộc CA Ban Cơ yếu Chính phủ không, dựa trên các Root/Sub-CA đã GHIM theo pin
    /// SHA-256 trong SignatureConstants (file .crt nằm ở thư mục Cert cạnh app).
    ///
    /// Tách riêng khỏi ICertificateProvider vì lý do BẢO MẬT, không phải cho gọn: khi chứng thư đến từ agent
    /// chạy ở máy client, server BẮT BUỘC tự dựng chain và KHÔNG được tin cờ tin cậy do client gửi lên.
    /// </summary>
    public interface ICertificateTrustValidator
    {
        /// <summary>
        /// Cert NGƯỜI KÝ có thuộc CA Ban Cơ yếu không, xét tại <paramref name="verificationTimeUtc"/>.
        /// Hai nhánh G1 và G2, chỉ cần đạt MỘT: không thuộc G1 thì xét G2 và ngược lại.
        /// G1/G2 là THẾ HỆ hạ tầng PKI (G1 cấp 2010-2030, G2 cấp 2018-2048), không phải phân theo cơ quan.
        /// Xét G1 trước vì token production hiện là G1.
        /// </summary>
        bool IsTrusted(X509Certificate2 signerCert, DateTime verificationTimeUtc);

        /// <summary>
        /// Dựng chain với trust anchor tuỳ biến = CHỈ <paramref name="root"/> đã ghim; không tin bất cứ gì
        /// trong store của Windows. Revocation offline nên không chạm khoá bí mật, không bật hộp thoại PIN.
        /// <paramref name="verificationTimeUtc"/> bắt buộc truyền: để mặc định về thời điểm hiện tại là từ
        /// chối oan chứng thư đã hết hạn nhưng hợp lệ tại thời điểm ký.
        /// </summary>
        bool ChainsToRoot(X509Certificate2 leaf, X509Certificate2Collection extra, X509Certificate2 root,
            DateTime verificationTimeUtc);

        /// <summary>
        /// Nạp một cert từ file (PEM hoặc DER) cạnh app rồi đối chiếu SHA-256 (DER) với pin.
        /// Trả null khi thiếu file hoặc SAI PIN - tráo file thì nạp fail chứ không âm thầm tin cert lạ.
        /// </summary>
        X509Certificate2? LoadPinnedCert(string fileName, string expectedSha256);
    }
}
