using AutoMapper;
using ksts.be.infrastructure.Persistence;
using ksts.be.shared.Constants;
using ksts.be.shared.Constants.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ksts.be.applications.Base
{
    public class BaseService
    {
        public readonly KstsDbContext _kstsDbContext;
        public readonly ILogger<BaseService> _logger;
        public readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly IMapper _mapper;
        public BaseService(KstsDbContext kstsDbContext, ILogger<BaseService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper)
        {
            _kstsDbContext = kstsDbContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
        }
        protected string getCurrentUserId()
        {
            var data = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(data))
            {
                data = _httpContextAccessor.HttpContext?.User.FindFirstValue(Claims.Subject);
            }
            //_logger.LogInformation($"getCurrentUserId: {data}");
            return data!;
        }
        protected string getCurrentName()
        {
            var data = _httpContextAccessor.HttpContext?.User.FindFirstValue(Claims.Name);
            return data!;
        }
        protected bool IsSuperAdmin()
        {
            var roles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).ToList();
            var isSuperAdmin = roles?.Any(r => r.Value == RoleConstants.ROLE_ADMIN) ?? false;
            return isSuperAdmin;
        }
        protected static DateTime GetVietnamTime()
        {
            return DateTimeConstants.VietnamNow;
        }
    }
}
