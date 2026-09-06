using AutoMapper;
using kssm.be.applications.Base;
using kssm.be.applications.Signing.Dtos;
using kssm.be.applications.Signing.Interfaces;
using kssm.be.external.Certificates.Dtos;
using kssm.be.external.Certificates.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.BaseRequest;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace kssm.be.applications.Signing.Implements
{
    public class CertificateService : BaseService, ICertificateService
    {
        private readonly ICertificateProvider _certificateProvider;

        public CertificateService(
            KssmDbContext kstsDbContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<BaseService> logger,
            ICertificateProvider certificateProvider
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _certificateProvider = certificateProvider;
        }

        public BaseResponsePagingDto<SignCertDto> GetCertificates(SignCertQueryDto query)
        {
            _logger.LogInformation($"{nameof(GetCertificates)} onlySignable={query.OnlySignable} pageNumber={query.PageNumber} pageSize={query.PageSize}");

            if (query.PageNumber <= 0)
            {
                query.PageNumber = 1;
            }

            if (query.PageSize <= 0)
            {
                query.PageSize = PagingParameter.DefaultPageSize;
            }

            var matches = _certificateProvider.GetCertificates().Certificates.AsEnumerable();

            if (query.OnlySignable)
            {
                matches = matches.Where(x => x.CanSign);
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                matches = matches.Where(x =>
                    x.CommonName.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase)
                    || x.IssuerCommonName.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase)
                    || x.SerialNumber.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase));
            }

            var matched = matches.ToList();

            return new BaseResponsePagingDto<SignCertDto>
            {
                Items = matched.AsQueryable().Paging(query).ToList(),
                TotalItems = matched.Count,
            };
        }

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
