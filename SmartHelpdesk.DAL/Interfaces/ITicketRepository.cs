using SmartHelpdesk.DAL.Entities;
using SmartHelpdesk.DAL.Entities.Ticket;

namespace SmartHelpdesk.DAL.Interfaces;

#region Interface
public interface ITicketRepository
{
    #region Get All Tickets
    Task<IReadOnlyList<Ticket>> GetAllAsync(
        CancellationToken cancellationToken = default);
    #endregion

    #region Get Ticket By Id
    Task<Ticket?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
    #endregion

    #region Get Tickets By Creator
    Task<IReadOnlyList<Ticket>> GetByCreatorUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);
    #endregion

    #region Get Tickets By Assignee
    Task<IReadOnlyList<Ticket>> GetAssignedToUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);
    #endregion

    #region Add Ticket
    Task<Ticket> AddAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default);
    #endregion

    #region Update Ticket
    Task UpdateAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default);
    #endregion

    #region Delete Ticket
    Task DeleteAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default);
    #endregion

    #region Ticket Comments

    Task<TicketComment> AddCommentAsync(
        TicketComment comment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketComment>> GetCommentsByTicketIdAsync(
        int ticketId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Ticket Status History

    Task<TicketStatusHistory> AddStatusHistoryAsync(
        TicketStatusHistory history,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketStatusHistory>> GetStatusHistoryByTicketIdAsync(
        int ticketId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Ticket Changes With Audit History

    Task<Ticket> AddWithHistoryAsync(
        Ticket ticket,
        TicketHistory history,
        CancellationToken cancellationToken = default);

    Task UpdateWithHistoryAsync(
        Ticket ticket,
        TicketHistory history,
        CancellationToken cancellationToken = default);

    Task<TicketComment> AddCommentWithHistoryAsync(
        TicketComment comment,
        TicketHistory history,
        CancellationToken cancellationToken = default);

    Task UpdateStatusWithHistoryAsync(
        Ticket ticket,
        TicketStatusHistory statusHistory,
        TicketHistory auditHistory,
        CancellationToken cancellationToken = default);

    #endregion
}
#endregion