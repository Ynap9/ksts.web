using ksts.be.external.Signing.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Signing.Implements
{
    /// <inheritdoc/>
    public class StoreSigningKey : ISigningKey
    {
        /// <inheritdoc/>
        public X509Certificate2 LayChungThu(string thumbprint)
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
                    return found;
                }
            }

            throw new UserFriendlyException(ErrorCodes.CertificateNotFound,
                "Không tìm thấy chứng thư số kèm khoá riêng theo vân tay đã chọn.");
        }

        /// <inheritdoc/>
        public byte[] Ky(byte[] duLieu, X509Certificate2 cert)
        {
            // Chỉ RA LỆNH ký, không đọc khoá: với token thì khoá nằm trong chip và middleware tự bật hộp PIN.
            using var rsa = cert.GetRSAPrivateKey()
                ?? throw new UserFriendlyException(ErrorCodes.CertificateCannotSign,
                    "Chứng thư số không dùng khoá RSA nên chưa ký được.");

            return rsa.SignData(duLieu, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
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
