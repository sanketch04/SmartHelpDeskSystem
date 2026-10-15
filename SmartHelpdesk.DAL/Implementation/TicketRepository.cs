
using Microsoft.EntityFrameworkCore;
using SmartHelpdesk.DAL.Context;
using SmartHelpdesk.DAL.Entities;
using SmartHelpdesk.DAL.Entities.Ticket;
using SmartHelpdesk.DAL.Interfaces;

namespace SmartHelpdesk.DAL.Implementation;

#region Repository

public sealed class TicketRepository : ITicketRepository
{
    #region Fields

    private readonly AppDbContext _context;

    #endregion

    #region Ctor

    public TicketRepository(AppDbContext context)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));
    }

    #endregion

    #region Get All Tickets

    public async Task<IReadOnlyList<Ticket>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .AsNoTracking()
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Get Ticket By Id

    public async Task<Ticket?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .FirstOrDefaultAsync(
                ticket => ticket.Id == id,
                cancellationToken);
    }

    #endregion

    #region Get Tickets By Creator

    public async Task<IReadOnlyList<Ticket>> GetByCreatorUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.CreatedByUserId == userId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Get Tickets By Assignee

    public async Task<IReadOnlyList<Ticket>> GetAssignedToUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.AssignedToUserId == userId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Add Ticket

    public async Task<Ticket> AddAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        await _context.Tickets.AddAsync(ticket, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return ticket;
    }

    #endregion

    #region Update Ticket

    public async Task UpdateAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        _context.Tickets.Update(ticket);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Delete Ticket

    public async Task DeleteAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        _context.Tickets.Remove(ticket);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Add Ticket Comment

    public async Task<TicketComment> AddCommentAsync(
        TicketComment comment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);

        await _context.TicketComments.AddAsync(
            comment,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return comment;
    }

    #endregion

    #region Get Ticket Comments

    public async Task<IReadOnlyList<TicketComment>>
        GetCommentsByTicketIdAsync(
            int ticketId,
            CancellationToken cancellationToken = default)
    {
        return await _context.TicketComments
            .AsNoTracking()
            .Include(comment => comment.User)
            .Where(comment => comment.TicketId == ticketId)
            .OrderBy(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Add Ticket Status History

    public async Task<TicketStatusHistory> AddStatusHistoryAsync(
        TicketStatusHistory history,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        await _context.TicketStatusHistories.AddAsync(
            history,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return history;
    }

    #endregion

    #region Get Ticket Status History

    public async Task<IReadOnlyList<TicketStatusHistory>>
        GetStatusHistoryByTicketIdAsync(
            int ticketId,
            CancellationToken cancellationToken = default)
    {
        return await _context.TicketStatusHistories
            .AsNoTracking()
            .Include(history => history.ChangedByUser)
            .Where(history => history.TicketId == ticketId)
            .OrderBy(history => history.ChangedAt)
            .ThenBy(history => history.Id)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Ticket Changes With Audit History

    /// <summary>
    /// Creates a ticket and its audit entry in one database transaction.
    /// </summary>
    public async Task<Ticket> AddWithHistoryAsync(
        Ticket ticket,
        TicketHistory history,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(history);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _context.Tickets.AddAsync(ticket, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            history.TicketId = ticket.Id;

            await _context.TicketHistories.AddAsync(
                history, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ticket;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Updates a ticket and its audit entry in one database transaction.
    /// </summary>
    public async Task UpdateWithHistoryAsync(
        Ticket ticket,
        TicketHistory history,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(history);

        if (ticket.Id <= 0 || history.TicketId != ticket.Id)
        {
            throw new ArgumentException(
                "The ticket and audit entry must reference the same valid ticket.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.Tickets.Update(ticket);

            await _context.TicketHistories.AddAsync(
                history, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Saves a comment and its audit entry in one database transaction.
    /// </summary>
    public async Task<TicketComment> AddCommentWithHistoryAsync(
        TicketComment comment,
        TicketHistory history,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);
        ArgumentNullException.ThrowIfNull(history);

        if (comment.TicketId <= 0 ||
            history.TicketId != comment.TicketId)
        {
            throw new ArgumentException(
                "The comment and audit entry must reference the same ticket.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _context.TicketComments.AddAsync(
                comment, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await _context.TicketHistories.AddAsync(
                history, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return comment;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Saves the ticket status change, legacy status history,
    /// and general audit entry atomically.
    /// </summary>
    public async Task UpdateStatusWithHistoryAsync(
        Ticket ticket,
        TicketStatusHistory statusHistory,
        TicketHistory auditHistory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(statusHistory);
        ArgumentNullException.ThrowIfNull(auditHistory);

        if (ticket.Id <= 0 ||
            statusHistory.TicketId != ticket.Id ||
            auditHistory.TicketId != ticket.Id)
        {
            throw new ArgumentException(
                "The ticket and both history entries must reference the same ticket.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.Tickets.Update(ticket);

            await _context.TicketStatusHistories.AddAsync(
                statusHistory, cancellationToken);

            await _context.TicketHistories.AddAsync(
                auditHistory, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    #endregion
}

#endregion
