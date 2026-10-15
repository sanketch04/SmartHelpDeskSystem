using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SmartHelpdesk.API.Security.Authorization
{
    public sealed class PermissionPolicyProvider
        : DefaultAuthorizationPolicyProvider
    {
        #region Constants

        private const string PermissionPrefix = "Permission:";

        #endregion

        #region Ctor

        public PermissionPolicyProvider(
            IOptions<AuthorizationOptions> options)
            : base(options)
        {
        }

        #endregion

        #region Policy Provider

        public override Task<AuthorizationPolicy?> GetPolicyAsync(
            string policyName)
        {
            if (!policyName.StartsWith(
                PermissionPrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                return base.GetPolicyAsync(policyName);
            }

            string permission = policyName[PermissionPrefix.Length..];

            if (string.IsNullOrWhiteSpace(permission))
            {
                return Task.FromResult<AuthorizationPolicy?>(null);
            }

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        #endregion
    }
}