using System.ComponentModel.DataAnnotations;

namespace SmartHelpdesk.Common.DTOs.TicketDtos;

#region Properties
public class UpdateTicketStatusDto
{
    [Required]
    [RegularExpression(
        "^(Open|In Progress|Pending|Resolved|Closed)$",
        ErrorMessage = "Status must be Open, In Progress, Pending, Resolved, or Closed.")]
    public string Status { get; set; } = "Open";
}
#endregion