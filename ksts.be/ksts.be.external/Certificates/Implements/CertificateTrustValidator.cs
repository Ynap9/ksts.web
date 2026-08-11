using ksts.be.external.Certificates.Interfaces;
using ksts.be.shared.Constants.Signing;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Certificates.Implements
{
    /// <summary>
    /// Dựng chain chứng thư về Root G1 / Root G2 đã ghim của Ban Cơ yếu Chính phủ.
    ///
    /// Cert ghim nạp MỘT LẦN lúc khởi tạo (service đăng ký Singleton): đọc lại file cho mỗi lời gọi là phí I/O
    /// mà không đổi kết quả. Thiếu file hoặc sai pin thì cert tương ứng là null và nhánh đó coi như không tin
    /// được - fail-closed.
    /// </summary>
    public class CertificateTrustValidator : ICertificateTrustValidator
    {
        private readonly X509Certificate2? _rootG1;
        private readonly X509Certificate2? _rootG2;
        private readonly X509Certificate2? _subCaG1;
        private readonly X509Certificate2Collection _intermediateCAs;
        private readonly ILogger<CertificateTrustValidator> _logger;

        public CertificateTrustValidator(ILogger<CertificateTrustValidator> logger)
        {
            _logger = logger;

            _rootG1 = LoadPinnedCert(SignatureConstants.RootG1FileName, SignatureConstants.RootG1Sha256);
            _rootG2 = LoadPinnedCert(SignatureConstants.RootG2FileName, SignatureConstants.RootG2Sha256);
            _subCaG1 = LoadPinnedCert(SignatureConstants.SubCaG1FileName, SignatureConstants.SubCaG1Sha256);

            _intermediateCAs = new X509Certificate2Collection();
            foreach (var (fileName, pin) in SignatureConstants.IntermediateCaPins)
            {
                var ca = LoadPinnedCert(fileName, pin);
                if (ca != null)
                {
                    _intermediateCAs.Add(ca);
                }
            }

            _logger.LogInformation(
                "Nạp CA đã ghim: RootG1={G1}, RootG2={G2}, SubCaG1={Sub}, CA trung gian={Count}",
                _rootG1 != null, _rootG2 != null, _subCaG1 != null, _intermediateCAs.Count);
        }

        /// <inheritdoc/>
        public bool IsTrusted(X509Certificate2 signerCert, DateTime verificationTimeUtc)
        {
            if (_rootG1 != null && _subCaG1 != null)
            {
                var extraG1 = new X509Certificate2Collection { _subCaG1 };
                if (ChainsToRoot(signerCert, extraG1, _rootG1, verificationTimeUtc))
                {
                    return true;
                }
            }

            return _rootG2 != null
                && ChainsToRoot(signerCert, new X509Certificate2Collection(), _rootG2, verificationTimeUtc);
        }

        /// <inheritdoc/>
        public bool ChainsToRoot(X509Certificate2 leaf, X509Certificate2Collection extra, X509Certificate2 root,
            DateTime verificationTimeUtc)
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(root);
            chain.ChainPolicy.ExtraStore.AddRange(extra);
            chain.ChainPolicy.ExtraStore.AddRange(_intermediateCAs);
            chain.ChainPolicy.VerificationTime = verificationTimeUtc;

            if (!chain.Build(leaf))
            {
                return false;
            }

            var top = chain.ChainElements[^1].Certificate;
            return string.Equals(top.Thumbprint, root.Thumbprint, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public X509Certificate2? LoadPinnedCert(string fileName, string expectedSha256)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory,
                    SignatureConstants.CertificateFolderName, fileName);
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppContext.BaseDirectory, fileName);
                }
                if (!File.Exists(path))
                {
                    return null;
                }

                var text = File.ReadAllText(path);
                var cert = text.Contains("BEGIN CERTIFICATE")
                    ? X509Certificate2.CreateFromPem(text)
                    : X509CertificateLoader.LoadCertificateFromFile(path);

                var sha = cert.GetCertHashString(HashAlgorithmName.SHA256);
                return string.Equals(sha, expectedSha256, StringComparison.OrdinalIgnoreCase) ? cert : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không nạp được chứng thư ghim {File}", fileName);
                return null;
            }
        }
    }
}
