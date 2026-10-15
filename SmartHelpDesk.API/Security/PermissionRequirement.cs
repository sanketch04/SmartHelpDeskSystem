using Microsoft.AspNetCore.Authorization;

namespace SmartHelpdesk.API.Security.Authorization
{
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        #region Properties

        public string Permission { get; }

        #endregion

        #region Ctor

        public PermissionRequirement(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                throw new ArgumentException(
                    "Permission cannot be empty.",
                    nameof(permission));
            }

            Permission = permission;
        }

        #endregion
    }
}