using SmartHelpdesk.DAL.Entities;

namespace SmartHelpdesk.DAL.Interfaces;

#region Interface
public interface ITicketHistoryRepository
{
    #region Add History
    Task<TicketHistory> AddAsync(
        TicketHistory history,
        CancellationToken cancellationToken = default);
    #endregion

    #region Get Ticket History
    Task<IReadOnlyList<TicketHistory>> GetByTicketIdAsync(
        int ticketId,
        CancellationToken cancellationToken = default);
    #endregion
}
#endregion