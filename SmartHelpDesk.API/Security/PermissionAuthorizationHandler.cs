using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartHelpdesk.DAL.Context;

namespace SmartHelpdesk.API.Security.Authorization
{
    public sealed class PermissionAuthorizationHandler
        : AuthorizationHandler<PermissionRequirement>
    {
        #region Fields

        private readonly AppDbContext _context;

        #endregion

        #region Ctor

        public PermissionAuthorizationHandler(AppDbContext context)
        {
            _context = context;
        }

        #endregion

        #region Authorization Handler

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            string? userId = context.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            bool hasPermission = await _context.UserRoles
                .AnyAsync(userRole =>
                    userRole.UserId == userId &&
                    _context.RolePermissions.Any(rolePermission =>
                        rolePermission.RoleId == userRole.RoleId &&
                        rolePermission.Permission.Name ==
                            requirement.Permission));

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }

        #endregion
    }
}