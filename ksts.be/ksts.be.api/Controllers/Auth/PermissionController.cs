using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using ksts.be.api.Controllers.Base;
using ksts.be.application.Auth.Interfaces;
using ksts.be.api.Attributes;
using ksts.be.shared.Constants.Auth;
using ksts.be.shared.Requests;


namespace ksts.be.api.Controllers.Auth
{
    [Route("api/app/permissions")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PermissionController : BaseController
    {
        private readonly IPermissionsService _permissionsService;
        public PermissionController(ILogger<BaseController> logger, IPermissionsService permissionsService) : base(logger)
        {
            _permissionsService = permissionsService;
        }

        /// <summary>
        /// Lấy toàn bộ permission
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        [Permission(PermissionKeys.RoleView)]
        public ApiResponse GetAll()
        {
            try
            {
                var data = _permissionsService.GetAllPermissions();
                return new(data);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
