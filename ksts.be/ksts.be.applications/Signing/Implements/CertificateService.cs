using AutoMapper;
using ksts.be.applications.Base;
using ksts.be.applications.Signing.Dtos;
using ksts.be.applications.Signing.Interfaces;
using ksts.be.external.Certificates.Dtos;
using ksts.be.external.Certificates.Interfaces;
using ksts.be.infrastructure.Persistence;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ksts.be.applications.Signing.Implements
{
    /// <inheritdoc/>
    public class CertificateService : BaseService, ICertificateService
    {
        private readonly ICertificateProvider _certificateProvider;

        public CertificateService(
            KstsDbContext kstsDbContext,
            ILogger<BaseService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ICertificateProvider certificateProvider
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _certificateProvider = certificateProvider;
        }

        /// <inheritdoc/>
        public List<SignCertDto> GetCertificates(SignCertQueryDto query)
        {
            _logger.LogInformation($"{nameof(GetCertificates)} onlySignable={query.OnlySignable}");

            var scan = _certificateProvider.GetCertificates();
            return query.OnlySignable
                ? scan.Certificates.Where(x => x.CanSign).ToList()
                : scan.Certificates;
        }

        /// <inheritdoc/>
        public CertDiagnosticDto GetDiagnostics()
        {
            _logger.LogInformation($"{nameof(GetDiagnostics)}");

            var scan = _certificateProvider.GetCertificates();
            return new CertDiagnosticDto
            {
                TotalCertificates = scan.Certificates.Count,
                SignableCertificates = scan.Certificates.Count(x => x.CanSign),
                StoreDiagnostics = scan.StoreDiagnostics,
            };
        }

        /// <inheritdoc/>
        public SignCertDto SelectCertificate(SelectCertDto input)
        {
            _logger.LogInformation($"{nameof(SelectCertificate)} thumbprint={input.Thumbprint}");

            if (string.IsNullOrWhiteSpace(input.Thumbprint))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Chưa chọn chứng thư số.");
            }

            var scan = _certificateProvider.GetCertificates();
            var cert = scan.Certificates.FirstOrDefault(x =>
                string.Equals(x.Thumbprint, input.Thumbprint.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new UserFriendlyException(ErrorCodes.CertificateNotFound,
                    "Không tìm thấy chứng thư số đã chọn. Kiểm tra lại token đã cắm chưa.");

            if (!cert.CanSign)
            {
                throw new UserFriendlyException(ErrorCodes.CertificateCannotSign,
                    cert.Reason ?? "Chứng thư số không đủ điều kiện để ký.");
            }

            return cert;
        }
    }
}
