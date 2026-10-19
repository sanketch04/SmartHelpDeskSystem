using System.ComponentModel.DataAnnotations;

namespace SmartHelpdesk.Common.DTOs.TicketDtos;

#region Properties
public class CreateTicketDto
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = "General";

    [Required]
    [RegularExpression("^(Low|Medium|High|Critical)$",
        ErrorMessage = "Priority must be Low, Medium, High, or Critical.")]
    public string Priority { get; set; } = "Medium";
    #region AI Assistance

    public string? AISummary { get; set; }

    #endregion
}
#endregion