using ksts.plugin.applications.ChungThuSo.Interfaces;
using ksts.plugin.external.Certificates.Dtos;
using ksts.plugin.external.Certificates.Interfaces;

namespace ksts.plugin.applications.ChungThuSo.Implements
{
    /// <inheritdoc/>
    public class ChungThuSoService : IChungThuSoService
    {
        private readonly ICertificateProvider _certificateProvider;
        private readonly ITokenVerifier _tokenVerifier;

        public ChungThuSoService(ICertificateProvider certificateProvider, ITokenVerifier tokenVerifier)
        {
            _certificateProvider = certificateProvider;
            _tokenVerifier = tokenVerifier;
        }

        /// <inheritdoc/>
        public CertScanResultDto GetList(bool onlySignable)
        {
            var result = _certificateProvider.GetCertificates();

            if (onlySignable)
            {
                result.Certificates = result.Certificates.Where(x => x.Reason == null).ToList();
            }

            return result;
        }

        /// <inheritdoc/>
        public TokenVerifyDto KiemTraToken(string thumbprint)
        {
            return _tokenVerifier.Verify(thumbprint);
        }
    }
}
