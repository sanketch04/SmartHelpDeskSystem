using System.ComponentModel.DataAnnotations;

namespace SmartHelpdesk.Common.DTOs.SupportAgents;

#region DTO
public class UpdateSupportAgentDto
{
    #region Properties

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;


    [Phone]
    public string? PhoneNumber { get; set; }

    #endregion
}
#endregion