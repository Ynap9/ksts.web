using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ksts.be.application.Auth.Interfaces;
using ksts.be.applications.Base;

using ksts.be.infrastructure.Persistence;
using ksts.be.application.Auth.Dtos.Permission;
using ksts.be.shared.Constants.Auth;


namespace ksts.be.application.Auth.Implements
{
    public class PermissionsService : BaseService, IPermissionsService
    {
        public PermissionsService(
            KstsDbContext kstsDbContext, ILogger<BaseService> logger, IHttpContextAccessor httpContextAccessor, IMapper mapper
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
        }

        public List<ViewPermissionDto> GetAllPermissions()
        {
            _logger.LogInformation($"{nameof(GetAllPermissions)}");

            var query = PermissionKeys.All.OrderBy(p => p).Select(x => new ViewPermissionDto
            {
                Key = x.Key,
                Name = x.Name,
                Category = x.Category
            }).ToList();

            return query;
        }
    }
}
