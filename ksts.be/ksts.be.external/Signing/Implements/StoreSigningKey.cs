using ksts.be.external.Signing.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Signing.Implements
{
    /// <summary>
    /// Nguồn ký đọc certificate store của MÁY ĐANG CHẠY API. Chỉ dùng được khi API và token nằm trên cùng
    /// một máy Windows, tức môi trường phát triển; trên máy chủ Linux nó không thấy chứng thư nào.
    /// Tham số loKyId không dùng tới vì nguồn này không có khái niệm phiên với máy người dùng.
    /// </summary>
    public class StoreSigningKey : ISigningKey
    {
        /// <inheritdoc/>
        public Task<X509Certificate2> LayChungThuAsync(int loKyId, string thumbprint,
            CancellationToken cancellationToken)
        {
            foreach (var location in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
            {
                using var store = new X509Store(StoreName.My, location);
                try
                {
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                }
                catch (CryptographicException)
                {
                    continue;
                }

                var found = store.Certificates
                    .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
                    .FirstOrDefault(c => c.HasPrivateKey);

                if (found != null)
                {
                    return Task.FromResult(found);
                }
            }

            throw new UserFriendlyException(ErrorCodes.CertificateNotFound,
                "Không tìm thấy chứng thư số kèm khoá riêng theo vân tay đã chọn.");
        }

        /// <inheritdoc/>
        public Task<byte[]> KyAsync(int loKyId, byte[] duLieu, X509Certificate2 cert,
            CancellationToken cancellationToken)
        {
            // Chỉ RA LỆNH ký, không đọc khoá: với token thì khoá nằm trong chip và middleware tự bật hộp PIN.
            using var rsa = cert.GetRSAPrivateKey()
                ?? throw new UserFriendlyException(ErrorCodes.CertificateCannotSign,
                    "Chứng thư số không dùng khoá RSA nên chưa ký được.");

            return Task.FromResult(rsa.SignData(duLieu, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
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
