using SmartHelpdesk.Common.DTOs.AI;

namespace SmartHelpdesk.BLL.Interfaces;

public interface IAiTicketService
{
    #region Methods

    Task<AITicketClassificationResponseDto> ClassifyTicketAsync(
        AITicketClassificationRequestDto request,
        CancellationToken cancellationToken = default);

    #endregion
}