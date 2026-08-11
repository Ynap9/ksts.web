using ksts.be.external.Certificates.Dtos;
using ksts.be.external.Certificates.Interfaces;
using ksts.be.shared.Constants.Signing;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Certificates.Implements
{
    /// <summary>
    /// Đọc chứng thư số từ Windows certificate store. Cert cá nhân, cert cấp máy và cert trên USB token đều
    /// nằm trong store (token được middleware bắc cầu qua minidriver) nên chỉ cần một đường đọc, không PKCS#11.
    /// </summary>
    public class CertificateProvider : ICertificateProvider
    {
        private static readonly (StoreLocation Location, CertSource Source)[] Stores =
        {
            (StoreLocation.CurrentUser, CertSource.Local),
            (StoreLocation.LocalMachine, CertSource.Server),
        };

        private readonly ICertificateTrustValidator _trustValidator;
        private readonly ILogger<CertificateProvider> _logger;

        public CertificateProvider(ICertificateTrustValidator trustValidator,
            ILogger<CertificateProvider> logger)
        {
            _trustValidator = trustValidator;
            _logger = logger;
        }

        /// <inheritdoc/>
        public CertScanResultDto GetCertificates()
        {
            _logger.LogInformation($"{nameof(GetCertificates)}");

            var result = new List<SignCertDto>();
            var diagnostics = new List<string>();
            var nowUtc = DateTime.UtcNow;

            foreach (var (location, defaultSource) in Stores)
            {
                var certs = new List<X509Certificate2>();
                try
                {
                    using var store = new X509Store(StoreName.My, location);
                    store.Open(OpenFlags.ReadOnly);
                    certs = store.Certificates.OfType<X509Certificate2>().ToList();
                    diagnostics.Add($"{location}\\My: {certs.Count} chứng thư");
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"{location}\\My: không mở được ({ex.GetType().Name}: {ex.Message})");
                    continue;
                }

                foreach (var cert in certs)
                {
                    string? keyProvider = null;
                    if (cert.HasPrivateKey)
                    {
                        try
                        {
                            using var rsa = cert.GetRSAPrivateKey();
                            if (rsa is RSACng rsaCng)
                            {
                                keyProvider = rsaCng.Key.Provider?.Provider;
                            }
#pragma warning disable SYSLIB0028 // CAPI đời cũ: một số middleware token đăng ký khoá qua CSP, không phải KSP.
                            else if (rsa is RSACryptoServiceProvider rsaCapi)
                            {
                                keyProvider = rsaCapi.CspKeyContainerInfo.ProviderName;
                            }
#pragma warning restore SYSLIB0028
                            else
                            {
                                using var ecdsa = cert.GetECDsaPrivateKey();
                                if (ecdsa is ECDsaCng ecdsaCng)
                                {
                                    keyProvider = ecdsaCng.Key.Provider?.Provider;
                                }
                            }
                        }
                        catch
                        {
                            keyProvider = null;
                        }
                    }

                    var isHardwareKey = !string.IsNullOrWhiteSpace(keyProvider)
                        && !SigningConstants.SoftwareKeyProviderMarkers.Any(marker =>
                            keyProvider.Contains(marker, StringComparison.OrdinalIgnoreCase))
                        && SigningConstants.HardwareKeyProviderMarkers.Any(marker =>
                            keyProvider.Contains(marker, StringComparison.OrdinalIgnoreCase));

                    var keyUsage = cert.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
                    var allowsSigning = keyUsage == null
                        || keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature)
                        || keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.NonRepudiation);

                    var dto = new SignCertDto
                    {
                        Subject = cert.Subject,
                        CommonName = cert.GetNameInfo(X509NameType.SimpleName, false) ?? string.Empty,
                        Issuer = cert.Issuer,
                        IssuerCommonName = cert.GetNameInfo(X509NameType.SimpleName, true) ?? string.Empty,
                        SerialNumber = cert.SerialNumber,
                        Thumbprint = cert.Thumbprint,
                        Source = isHardwareKey ? CertSource.UsbToken : defaultSource,
                        KeyProvider = keyProvider,
                        ValidFrom = cert.NotBefore.ToString("dd/MM/yyyy HH:mm:ss"),
                        ValidTo = cert.NotAfter.ToString("dd/MM/yyyy HH:mm:ss"),
                        HasPrivateKey = cert.HasPrivateKey,
                        IsExpired = nowUtc < cert.NotBefore.ToUniversalTime()
                            || nowUtc > cert.NotAfter.ToUniversalTime(),
                        AllowsSigning = allowsSigning,
                        IsTrusted = _trustValidator.IsTrusted(cert, nowUtc),
                    };

                    dto.Reason = !dto.HasPrivateKey
                        ? "Chứng thư số không có khoá bí mật trên máy này (chỉ xem được, không ký được)."
                        : dto.IsExpired
                            ? $"Chứng thư số ngoài thời hạn hiệu lực ({dto.ValidFrom} - {dto.ValidTo})."
                            : !dto.AllowsSigning
                                ? "Chứng thư số không được cấp quyền ký (KeyUsage thiếu digitalSignature/nonRepudiation)."
                                : !dto.IsTrusted
                                    ? "Chứng thư số không thuộc CA Ban Cơ yếu (chain không về Root G1 lẫn Root G2 đã ghim)."
                                    : null;

                    result.Add(dto);
                }
            }

            return new CertScanResultDto
            {
                Certificates = result
                    .GroupBy(c => c.Thumbprint, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(c => c.CanSign).First())
                    .OrderByDescending(c => c.CanSign)
                    .ThenBy(c => c.CommonName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
                StoreDiagnostics = diagnostics,
            };
        }
    }
}
