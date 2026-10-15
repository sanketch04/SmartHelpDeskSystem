
using System.ComponentModel.DataAnnotations;

namespace SmartHelpdesk.Common.DTOs.Tickets;

public class CreateTicketCommentDto
{
    #region Properties

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string CommentText { get; set; } = string.Empty;

    #endregion
}
