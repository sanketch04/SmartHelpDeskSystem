using System.ComponentModel.DataAnnotations;

namespace SmartHelpdesk.Common.DTOs.TicketDtos;

#region Properties
public class UpdateTicketDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Priority { get; set; } = "Medium";
}
#endregion