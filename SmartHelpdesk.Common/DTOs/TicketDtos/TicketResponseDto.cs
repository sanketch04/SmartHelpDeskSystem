namespace SmartHelpdesk.Common.DTOs.TicketDtos;

#region DTO
public class TicketResponseDto
{
    #region Properties
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? AssignedToUserId { get; set; }
    #endregion
}
#endregion