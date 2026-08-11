using ksts.be.external.Signing.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Signing.Implements
{
    /// <summary>
    /// Nguồn ký thật: khoá nằm trong chip token cắm ở máy người dùng, máy chủ chỉ gửi SignedAttributes đi và
    /// nhận chữ ký về qua <see cref="IHangDoiKy"/>.
    ///
    /// Máy chủ không giữ và không cần khoá bí mật lẫn mã PIN. Chứng thư nhận từ máy người dùng chỉ là phần
    /// CÔNG KHAI, và chuỗi tin cậy do máy chủ TỰ dựng — không tin cờ nào do máy người dùng gửi lên, vì máy đó
    /// không kiểm soát được.
    /// </summary>
    public class PluginSigningKey : ISigningKey
    {
        private readonly IHangDoiKy _hangDoiKy;

        public PluginSigningKey(IHangDoiKy hangDoiKy)
        {
            _hangDoiKy = hangDoiKy;
        }

        /// <inheritdoc/>
        public Task<X509Certificate2> LayChungThuAsync(int loKyId, string thumbprint,
            CancellationToken cancellationToken)
        {
            var cert = _hangDoiKy.LayChungThu(loKyId);

            // Chứng thư của phiên phải ĐÚNG cái người dùng chọn: lệch nghĩa là màn hình và token đang nói về
            // hai chứng thư khác nhau, ký tiếp là ra file mang tên người khác.
            if (!string.Equals(cert.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException(ErrorCodes.CertificateNotFound,
                    "Chứng thư của phiên ký không khớp chứng thư đã chọn cho lô.");
            }

            return Task.FromResult(cert);
        }

        /// <inheritdoc/>
        public Task<byte[]> KyAsync(int loKyId, byte[] duLieu, X509Certificate2 cert,
            CancellationToken cancellationToken)
        {
            return _hangDoiKy.XinChuKyAsync(loKyId, duLieu, cancellationToken);
        }

        /// <inheritdoc/>
        public IReadOnlyList<X509Certificate2> LayChuoiChungThu(X509Certificate2 cert)
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;

            // Dựng chain hỏng KHÔNG chặn việc ký: chuỗi chỉ để nhúng vào CMS cho khâu verify về sau, thiếu
            // mắt xích trung gian thì file vẫn ký được, chỉ là người kiểm phải tự nạp CA.
            if (!chain.Build(cert))
            {
                return new[] { cert };
            }

            return chain.ChainElements.Select(e => e.Certificate).ToList();
        }
    }
}
