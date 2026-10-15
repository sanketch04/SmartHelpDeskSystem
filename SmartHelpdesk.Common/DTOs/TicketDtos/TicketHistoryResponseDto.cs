namespace SmartHelpdesk.Common.DTOs.Tickets;

#region DTO

public sealed class TicketHistoryResponseDto
{
    #region Properties

    public int Id { get; set; }

    public int TicketId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string PerformedByUserId { get; set; } = string.Empty;

    public string? PerformedByUserName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    #endregion
}

#endregion