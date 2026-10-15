using SmartHelpdesk.Common.DTOs.TicketDtos;
using SmartHelpdesk.Common.DTOs.Tickets;

namespace SmartHelpdesk.BLL.Interfaces;

#region Interface

public interface ITicketService
{
    #region Get All Tickets

    Task<IReadOnlyList<TicketResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    #endregion

    #region Get Ticket By Id

    Task<TicketResponseDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    #endregion

    #region Get My Tickets

    Task<IReadOnlyList<TicketResponseDto>> GetMyTicketsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Get Assigned Tickets

    Task<IReadOnlyList<TicketResponseDto>> GetAssignedTicketsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Create Ticket

    Task<TicketResponseDto> CreateAsync(
        CreateTicketDto dto,
        string currentUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Update Ticket

    Task<TicketResponseDto?> UpdateAsync(
        int id,
        UpdateTicketDto dto,
        string updatedByUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Update Ticket Status

    Task<TicketResponseDto?> UpdateStatusAsync(
        int id,
        string status,
        string changedByUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Delete Ticket

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);

    #endregion

    #region Assign Ticket

    Task<TicketResponseDto?> AssignAsync(
        int ticketId,
        string assignedToUserId,
        string assignedByUserId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Ticket Comments

    Task<TicketCommentResponseDto?> AddCommentAsync(
        int ticketId,
        string userId,
        CreateTicketCommentDto dto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketCommentResponseDto>?> GetCommentsAsync(
        int ticketId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Ticket Status History

    Task<IReadOnlyList<TicketStatusHistoryResponseDto>?> GetStatusHistoryAsync(
        int ticketId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Ticket Audit History

    Task<IReadOnlyList<TicketHistoryResponseDto>?> GetAuditHistoryAsync(
        int ticketId,
        CancellationToken cancellationToken = default);

    #endregion
}

#endregion