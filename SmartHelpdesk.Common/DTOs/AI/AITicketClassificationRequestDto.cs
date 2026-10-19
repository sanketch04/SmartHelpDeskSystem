
using System.ComponentModel.DataAnnotations;

namespace SmartHelpdesk.Common.DTOs.AI;

public class AITicketClassificationRequestDto
{
    #region Properties

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    #endregion
}
