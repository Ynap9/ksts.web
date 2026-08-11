using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using ksts.be.api.Controllers.Base;
using ksts.be.application.Auth.Interfaces;
using ksts.be.shared.Constants.Auth;
using ksts.be.api.Attributes;
using ksts.be.shared.Requests;
using ksts.be.application.Auth.Dtos.Role;


namespace ksts.be.api.Controllers.Auth
{
    [Route("api/app/roles")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class RolesController : BaseController
    {
        private readonly IRoleService _roleService;

        public RolesController(ILogger<BaseController> logger, IRoleService roleService) : base(logger)
        {
            _roleService = roleService;
        }

        [Permission(PermissionKeys.RoleAdd)]
        [HttpPost("")]
        public async Task<ApiResponse> Create([FromBody] CreateRoleDto dto)
        {
            try
            {
                await _roleService.Create(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.RoleUpdate)]
        [HttpPut("")]
        public async Task<ApiResponse> Update([FromBody] UpdateRoleDto dto)
        {
            try
            {
                await _roleService.Update(dto);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.RoleView)]
        [HttpGet("")]
        public async Task<ApiResponse> Find([FromQuery] FindPagingRoleDto dto)
        {
            try
            {
                var data = await _roleService.FindPaging(dto);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }


        [Permission(PermissionKeys.RoleView)]
        [HttpGet("{id}")]
        public async Task<ApiResponse> GetById([FromRoute] string id)
        {
            try
            {
                var data = await _roleService.FindById(id);
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [Permission(PermissionKeys.RoleView)]
        [HttpGet("list")]
        public async Task<ApiResponse> GetList()
        {
            try
            {
                var data = await _roleService.GetList();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
