using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartHelpdesk.DAL.Context;
using SmartHelpdesk.DAL.Entities;

namespace SmartHelpdesk.DAL.Seed
{
    public static class DatabaseSeeder
    {
        #region Role Definitions

        private static readonly string[] Roles =
        {
            "Administrator",
            "SupportAgent",
            "Employee"
        };

        #endregion

        #region Permission Definitions

        private static readonly (string Name, string Description)[] Permissions =
        {
            ("Tickets.Create", "Create new tickets"),
            ("Tickets.ViewOwn", "View own tickets"),
            ("Tickets.ViewAll", "View all tickets"),
            ("Tickets.Assign", "Assign tickets to support agents"),
            ("Tickets.UpdateStatus", "Update ticket status"),
            ("Tickets.Delete", "Delete tickets"),
            ("Users.Manage", "Manage users"),
            ("Roles.Manage", "Manage roles and permissions"),
            ("Reports.View", "View system reports")
        };

        #endregion

        #region Public Methods

        public static async Task SeedAsync(
            AppDbContext context,
            RoleManager<IdentityRole> roleManager)
        {
            await SeedRolesAsync(roleManager);
            await SeedPermissionsAsync(context);
            await SeedRolePermissionsAsync(context, roleManager);
        }

        #endregion

        #region Role Seeding

        private static async Task SeedRolesAsync(
            RoleManager<IdentityRole> roleManager)
        {
            foreach (string roleName in Roles)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(
                    new IdentityRole(roleName));

                if (!result.Succeeded)
                {
                    string errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Unable to create role '{roleName}': {errors}");
                }
            }
        }

        #endregion

        #region Permission Seeding

        private static async Task SeedPermissionsAsync(
            AppDbContext context)
        {
            foreach (var item in Permissions)
            {
                bool exists = await context.Permissions
                    .AnyAsync(p => p.Name == item.Name);

                if (!exists)
                {
                    context.Permissions.Add(new Permission
                    {
                        Name = item.Name,
                        Description = item.Description
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        #endregion

        #region Role Permission Mapping

        private static async Task SeedRolePermissionsAsync(
            AppDbContext context,
            RoleManager<IdentityRole> roleManager)
        {
            var adminRole = await roleManager.FindByNameAsync(
                "Administrator")
                ?? throw new InvalidOperationException(
                    "Administrator role was not found.");

            var supportRole = await roleManager.FindByNameAsync(
                "SupportAgent")
                ?? throw new InvalidOperationException(
                    "SupportAgent role was not found.");

            var employeeRole = await roleManager.FindByNameAsync(
                "Employee")
                ?? throw new InvalidOperationException(
                    "Employee role was not found.");

            var permissionIds = await context.Permissions
                .ToDictionaryAsync(p => p.Name, p => p.Id);

            var mappings = new Dictionary<string, string[]>
            {
                ["Administrator"] = Permissions
                    .Select(p => p.Name)
                    .ToArray(),

                ["SupportAgent"] = new[]
                {
                    "Tickets.ViewAll",
                    "Tickets.Assign",
                    "Tickets.UpdateStatus",
                    "Reports.View"
                },

                ["Employee"] = new[]
                {
                    "Tickets.Create",
                    "Tickets.ViewOwn"
                }
            };

            var roleIds = new Dictionary<string, string>
            {
                ["Administrator"] = adminRole.Id,
                ["SupportAgent"] = supportRole.Id,
                ["Employee"] = employeeRole.Id
            };

            foreach (var mapping in mappings)
            {
                string roleId = roleIds[mapping.Key];

                foreach (string permissionName in mapping.Value)
                {
                    if (!permissionIds.TryGetValue(
                        permissionName, out int permissionId))
                    {
                        throw new InvalidOperationException(
                            $"Permission '{permissionName}' was not found.");
                    }

                    bool exists = await context.RolePermissions
                        .AnyAsync(rp =>
                            rp.RoleId == roleId &&
                            rp.PermissionId == permissionId);

                    if (!exists)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = roleId,
                            PermissionId = permissionId
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        #endregion
    }
}