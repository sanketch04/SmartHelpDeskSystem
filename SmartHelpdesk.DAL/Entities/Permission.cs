using System;
using System.Collections.Generic;

namespace SmartHelpdesk.DAL.Entities
{
    public class Permission
    {
        #region Properties

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        public ICollection<RolePermission> RolePermissions { get; set; }
            = new List<RolePermission>();

        #endregion
    }
}