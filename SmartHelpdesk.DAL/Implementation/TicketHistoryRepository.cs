using Microsoft.EntityFrameworkCore;
using SmartHelpdesk.DAL.Context;
using SmartHelpdesk.DAL.Entities;
using SmartHelpdesk.DAL.Interfaces;

namespace SmartHelpdesk.DAL.Repository;

#region Repository
public sealed class TicketHistoryRepository : ITicketHistoryRepository
{
    #region Fields
    private readonly AppDbContext _context;
    #endregion

    #region Ctor
    public TicketHistoryRepository(AppDbContext context)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));
    }
    #endregion

    #region Add History
    public async Task<TicketHistory> AddAsync(
        TicketHistory history,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (history.TicketId <= 0)
        {
            throw new ArgumentException(
                "A valid TicketId is required.",
                nameof(history));
        }

        if (string.IsNullOrWhiteSpace(history.PerformedByUserId))
        {
            throw new ArgumentException(
                "The acting user is required.",
                nameof(history));
        }

        if (string.IsNullOrWhiteSpace(history.Action))
        {
            throw new ArgumentException(
                "The history action is required.",
                nameof(history));
        }

        history.Action = history.Action.Trim();
        history.PerformedByUserId = history.PerformedByUserId.Trim();
        history.Description = history.Description.Trim();

        // The server determines the audit timestamp.
        history.CreatedAt = DateTime.UtcNow;

        await _context.TicketHistories.AddAsync(
            history,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return history;
    }
    #endregion

    #region Get Ticket History
    public async Task<IReadOnlyList<TicketHistory>> GetByTicketIdAsync(
        int ticketId,
        CancellationToken cancellationToken = default)
    {
        if (ticketId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ticketId),
                "Ticket ID must be greater than zero.");
        }

        return await _context.TicketHistories
            .AsNoTracking()
            .Where(history => history.TicketId == ticketId)
            .Include(history => history.PerformedByUser)
            .OrderByDescending(history => history.CreatedAt)
            .ThenByDescending(history => history.Id)
            .ToListAsync(cancellationToken);
    }
    #endregion
}
#endregion