using ksts.be.infrastructure.Persistence;
using ksts.be.shared.Constants.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace ksts.be.api.Attributes
{
    public class PermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public string Permission { get; }
        public PermissionAttribute(string permission)
        {
            Permission = permission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity!.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var username = user.FindFirstValue("username");

            var dbContext = context.HttpContext
                .RequestServices
                .GetService(typeof(KstsDbContext)) as KstsDbContext;

            if (dbContext != null)
            {
                // check role super admin
                var isSuperAdmin = (
                                    from u in dbContext.Users
                                    join userRole in dbContext.UserRoles on u.Id equals userRole.UserId
                                    join role in dbContext.Roles on userRole.RoleId equals role.Id
                                    where u.UserName == username
                                      && role.Name == RoleConstants.ROLE_ADMIN
                                    select role.Name).Any();
                if (isSuperAdmin) 
                { 
                    return;
                }

                // check per
                var isPermit = (
                        from u in dbContext.Users
                        join userRole in dbContext.UserRoles on u.Id equals userRole.UserId
                        join role in dbContext.Roles on userRole.RoleId equals role.Id
                          join roleClaims in dbContext.RoleClaims on role.Id equals roleClaims.RoleId
                          where u.UserName == username
                            && roleClaims.ClaimType == CustomClaimTypes.Permission
                            && roleClaims.ClaimValue == Permission
                          select roleClaims.ClaimValue).Any();

                if (isPermit)
                {
                    return;
                }
            }


            //Return based on logic
            context.Result = new ForbidResult();
        }
    }
}
