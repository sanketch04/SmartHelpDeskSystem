using Microsoft.AspNetCore.Identity;

namespace SmartHelpdesk.DAL.Entities;

public class ApplicationUser : IdentityUser
{
    #region Properties
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? SupportAgentCode { get; set; }

    public string? ProfileImageUrl { get; set; }

    #endregion
}
