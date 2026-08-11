using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

using ksts.be.application.Auth.Interfaces;
using ksts.be.applications.Base;

using ksts.be.infrastructure.Persistence;
using ksts.be.application.Auth.Dtos.Role;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using ksts.be.shared.Constants.Auth;
using ksts.be.shared.HttpRequest.BaseRequest;



namespace ksts.be.application.Auth.Implements
{
    public class RoleService : BaseService, IRoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        public RoleService(
            KstsDbContext kstsDbContext, 
            ILogger<BaseService> logger, 
            IHttpContextAccessor httpContextAccessor, 
            IMapper mapper,
            RoleManager<IdentityRole> roleManager
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _roleManager = roleManager;
        }

        public async Task Create(CreateRoleDto dto)
        {
            _logger.LogInformation($"{nameof(Create)} dto={JsonSerializer.Serialize(dto)}");

            var trans = await _kstsDbContext.Database.BeginTransactionAsync();
            var role = new IdentityRole
            {
                Name = dto.Name,
                NormalizedName = dto.Name.ToUpper(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorCreateRole, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            foreach (var per in dto.PermissionKey)
            {
                var claim = new Claim(CustomClaimTypes.Permission, per);
                var claimResult = await _roleManager.AddClaimAsync(role, claim);
            }

            await _kstsDbContext.SaveChangesAsync();
            await trans.CommitAsync();
        }

        public async Task<ViewRoleDto> FindById(string id)
        {
            _logger.LogInformation($"{nameof(FindById)} id={id}");

            var role = await _roleManager.FindByIdAsync(id);
            var data = _mapper.Map<ViewRoleDto>(role);

            if (role != null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                data.PermissionKey = claims
                                        .Where(c => c.Type == CustomClaimTypes.Permission)
                                        .Select(c => c.Value)
                                        .ToList();
            }

            return data;
        }

        public async Task<BaseResponsePagingDto<ViewRoleDto>> FindPaging(FindPagingRoleDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)} dto={JsonSerializer.Serialize(dto)}");

            var query = _roleManager.Roles.AsNoTracking().AsQueryable();
            var data = await query.OrderBy(x => x.Name)
                        .Paging(dto)
                        .ToListAsync();

            var items = _mapper.Map<List<ViewRoleDto>>(data);
            foreach (var item in items)
            {
                var roleClaims = _kstsDbContext.RoleClaims
                                    .Where(rc => rc.RoleId == item.Id && rc.ClaimType == CustomClaimTypes.Permission)
                                    .Select(rc => rc.ClaimValue)
                                    .ToList();
                if (roleClaims != null)
                {
                    item.PermissionKey = roleClaims!;
                }
            }

            return new BaseResponsePagingDto<ViewRoleDto>
            {
                Items = items,
                TotalItems = query.Count(),
            };
        }

        public async Task Update(UpdateRoleDto dto)
        {
            _logger.LogInformation($"{nameof(Update)} dto={JsonSerializer.Serialize(dto)}");

            var role = await _roleManager.FindByIdAsync(dto.Id)
                            ?? throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            
            var trans = await _kstsDbContext.Database.BeginTransactionAsync();
            var oldRoleClaims = await _kstsDbContext.RoleClaims
                                    .Where(rc => rc.RoleId == role.Id)
                                    .ToListAsync();

            _kstsDbContext.RoleClaims.RemoveRange(oldRoleClaims);

            var newRoleClaims = new List<IdentityRoleClaim<string>>();
            foreach (var per in dto.PermissionKey)
            {
                var claim = new IdentityRoleClaim<string>
                {
                    RoleId = role.Id,
                    ClaimType = CustomClaimTypes.Permission,
                    ClaimValue = per
                };
                newRoleClaims.Add(claim);
            }

            _kstsDbContext.RoleClaims.AddRange(newRoleClaims);

            await _kstsDbContext.SaveChangesAsync();
            await trans.CommitAsync();
        }

        public async Task<List<ViewRoleDto>> GetList() {
            _logger.LogInformation($"{nameof(GetList)}");

            var query = _roleManager.Roles.AsNoTracking().AsQueryable();
            var data = await query.OrderBy(x => x.Name)
                        .ToListAsync();

            var result = _mapper.Map<List<ViewRoleDto>>(data);

            return result;
        }
    }
}
