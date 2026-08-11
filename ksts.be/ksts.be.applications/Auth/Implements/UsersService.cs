using AutoMapper;
using ksts.be.application.Auth.Dtos.User;
using ksts.be.application.Auth.Interfaces;
using ksts.be.applications.Base;
using ksts.be.domain.Auth;
using ksts.be.infrastructure.Persistence;
using ksts.be.shared.Constants.Auth;
using ksts.be.shared.HttpRequest.BaseRequest;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using ksts.be.shared.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using thongbao.be.application.Auth.Dtos.User;



namespace ksts.be.application.Auth.Implements
{
    public class UsersService : BaseService, IUsersService
    {
        private readonly UserManager<AppUser> _userManager;

        public UsersService(
            KstsDbContext kstsDbContext, ILogger<BaseService> logger, IHttpContextAccessor httpContextAccessor, IMapper mapper,
            UserManager<AppUser> userManager
        ) : base(kstsDbContext, logger, httpContextAccessor, mapper)
        {
            _userManager = userManager;
        }

        public async Task<ViewUserDto> Create(CreateUserDto dto)
        {
            _logger.LogInformation($"{nameof(Create)} dto={JsonSerializer.Serialize(dto)}");

            using var transaction = await _kstsDbContext.Database.BeginTransactionAsync();

            bool isRandomPassword = string.IsNullOrEmpty(dto.Password);
            if (isRandomPassword)
            {
                dto.Password = CryptoUtils.GenerateRandomString(8);
            }

            var newUser = new AppUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.FullName ?? "",
                PhoneNumber = dto.PhoneNumber,
                //MsAccount = dto.MsAccount ?? "",
            };
            var result = await _userManager.CreateAsync(newUser, dto.Password);

            if (!result.Succeeded)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorCreateUser, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRolesAsync(newUser, dto.RoleNames);

            await _kstsDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var rslt = _mapper.Map<ViewUserDto>(newUser);

            if (isRandomPassword)
            {
                rslt.PasswordRandom = dto.Password;
            }

            return rslt;
        }

        public async Task<ViewUserDto> FindById(string id)
        {
            _logger.LogInformation($"{nameof(FindById)} id={id}");

            var user = await _userManager.FindByIdAsync(id);

            var roles = await _userManager.GetRolesAsync(user);

            var data = _mapper.Map<ViewUserDto>(user);
            data.Roles = roles;
            return data;
        }

        /*public async Task<ViewUserDto> FindByMsAccount(string msAccount)
        {
            _logger.LogInformation($"{nameof(FindByMsAccount)} id={msAccount}");

            var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(x => x.MsAccount == msAccount)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            return _mapper.Map<ViewUserDto>(user);
        }*/

        public async Task<BaseResponsePagingDto<ViewUserDto>> FindPaging(FindPagingUserDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)} dto={JsonSerializer.Serialize(dto)}");

            var query = _userManager.Users.AsNoTracking().AsQueryable();

            var totalCount = await query.CountAsync();

            var users = await query
                    .OrderBy(x => x.UserName)
                    .Paging(dto)
                    .ToListAsync();
            var items = _mapper.Map<List<ViewUserDto>>(users);

            return new BaseResponsePagingDto<ViewUserDto>
            {
                Items = items,
                TotalItems = totalCount,
            };
        }

        public async Task SetRoleForUser(SetRoleForUserDto dto)
        {
            _logger.LogInformation($"{nameof(SetRoleForUser)} dto={JsonSerializer.Serialize(dto)}");

            var transaction = await _kstsDbContext.Database.BeginTransactionAsync();
            var user = await _userManager.FindByIdAsync(dto.Id)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            await _userManager.RemoveFromRolesAsync(user, (await _userManager.GetRolesAsync(user)).ToArray());
            await _userManager.AddToRolesAsync(user, dto.RoleNames);
            await _kstsDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task Update(UpdateUserDto dto)
        {
            _logger.LogInformation($"{nameof(Update)} dto={JsonSerializer.Serialize(dto)}");

            var user = await _userManager.FindByIdAsync(dto.Id)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);

            var transaction = await _kstsDbContext.Database.BeginTransactionAsync();
            user.FullName = dto.FullName ?? user.FullName;
            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
            user.Email = dto.Email ?? user.Email;

            await _userManager.UpdateAsync(user);
            await _userManager.RemoveFromRolesAsync(user, (await _userManager.GetRolesAsync(user)).ToArray());
            await _userManager.AddToRolesAsync(user, dto.RoleNames);

            await _kstsDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task<ViewMeDto> GetMe()
        {
            _logger.LogInformation($"{nameof(GetMe)}");
            var userId = getCurrentUserId();
            
            var data = await _userManager.FindByIdAsync(userId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            var user = _mapper.Map<ViewMeDto>(data);

            var roles = await _userManager.GetRolesAsync(data);
            user.Roles = roles;

            var permissions = (from u in _kstsDbContext.Users
                               join userRole in _kstsDbContext.UserRoles on u.Id equals userRole.UserId
                               join role in _kstsDbContext.Roles on userRole.RoleId equals role.Id
                               join roleClaims in _kstsDbContext.RoleClaims on role.Id equals roleClaims.RoleId
                               where u.Id == userId
                                 && roleClaims.ClaimType == CustomClaimTypes.Permission
                               select roleClaims.ClaimValue).ToList();
            user.Permissions = permissions;

            return user;
        }
        public List<GetListUserDto> GetListUser()
        {
            _logger.LogInformation($"{nameof(GetListUser)}");

            var superAdminRoleId = _kstsDbContext.Roles
                .AsNoTracking()
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefault();

            var superAdminUserIds = _kstsDbContext.UserRoles
                .AsNoTracking()
                .Where(ur => ur.RoleId == superAdminRoleId)
                .Select(ur => ur.UserId)
                .ToList();

            var users = _userManager.Users
                .AsNoTracking()
                .Where(u => !superAdminUserIds.Contains(u.Id))
                .Select(u => new GetListUserDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    Email = u.Email ?? "",
                    FullName = u.FullName
                })
                .OrderBy(x => x.UserName)
                .ToList();

            return users;
        }

    }
}
