
namespace SmartHelpdesk.Common.DTOs.Tickets;

public class TicketCommentResponseDto
{
    #region Properties

    public int Id { get; set; }

    public int TicketId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string CommentText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    #endregion
}
