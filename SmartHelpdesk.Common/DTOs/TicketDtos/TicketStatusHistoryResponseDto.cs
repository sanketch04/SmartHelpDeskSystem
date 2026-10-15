
namespace SmartHelpdesk.Common.DTOs.Tickets;

public class TicketStatusHistoryResponseDto
{
    #region Properties

    public int Id { get; set; }

    public int TicketId { get; set; }

    public string OldStatus { get; set; } = string.Empty;

    public string NewStatus { get; set; } = string.Empty;

    public string ChangedByUserId { get; set; } = string.Empty;

    public string? ChangedByUserName { get; set; }

    public DateTime ChangedAt { get; set; }

    #endregion
}
