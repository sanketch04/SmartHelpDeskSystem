
namespace SmartHelpdesk.Common.DTOs.AI;

public class AITicketClassificationResponseDto
{
    #region Properties

    public string SuggestedCategory { get; set; } = "General";

    public string SuggestedPriority { get; set; } = "Medium";

    public string Summary { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    #endregion
}
