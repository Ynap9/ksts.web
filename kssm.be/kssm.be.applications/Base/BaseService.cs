using AutoMapper;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants;
using kssm.be.shared.Constants.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace kssm.be.applications.Base
{
    public class BaseService
    {
        public readonly KssmDbContext _kstsDbContext;
        public readonly ILogger<BaseService> _logger;
        public readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly IMapper _mapper;
        public BaseService(KssmDbContext kstsDbContext, ILogger<BaseService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper)
        {
            _kstsDbContext = kstsDbContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
        }
     
        protected static DateTime GetVietnamTime()
        {
            return DateTimeConstants.VietnamNow;
        }
    }
}
