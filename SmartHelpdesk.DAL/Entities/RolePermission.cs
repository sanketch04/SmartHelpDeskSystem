using Microsoft.AspNetCore.Identity;

namespace SmartHelpdesk.DAL.Entities
{
    public class RolePermission
    {
        #region Properties

        public string RoleId { get; set; } = string.Empty;

        public int PermissionId { get; set; }

        #endregion

        #region Navigation Properties

        public IdentityRole Role { get; set; } = null!;

        public Permission Permission { get; set; } = null!;

        #endregion
    }
}