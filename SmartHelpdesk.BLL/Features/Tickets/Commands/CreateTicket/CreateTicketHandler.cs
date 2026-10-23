using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using SmartHelpdesk.BLL.Common.CQRS;
using SmartHelpdesk.DAL.Entities;
using SmartHelpdesk.DAL.Entities.Ticket;
using SmartHelpdesk.DAL.Interfaces;

namespace SmartHelpdesk.BLL.Features.Tickets.Commands.CreateTicket;

public sealed class CreateTicketHandler
    : ICommandHandler<CreateTicketCommand, int>
{
    #region Fields

    private readonly ITicketRepository _ticketRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly HashSet<string> AllowedPriorities =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Low", "Medium", "High", "Critical"
        };

    #endregion

    #region Ctor

    public CreateTicketHandler(
        ITicketRepository ticketRepository,
        UserManager<ApplicationUser> userManager)
    {
        _ticketRepository = ticketRepository
            ?? throw new ArgumentNullException(nameof(ticketRepository));

        _userManager = userManager
            ?? throw new ArgumentNullException(nameof(userManager));
    }

    #endregion

    #region Handle

    public async Task<int> HandleAsync(
        CreateTicketCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateUserId(command.CreatedByUserId);

        var creator = await _userManager.FindByIdAsync(command.CreatedByUserId);

        if (creator is null || !creator.IsActive)
        {
            throw new InvalidOperationException(
                "The authenticated user does not exist or is inactive.");
        }

        ValidateRequiredText(command.Title, nameof(command.Title));
        ValidateRequiredText(command.Description, nameof(command.Description));
        ValidateRequiredText(command.Category, nameof(command.Category));

        var ticket = new Ticket
        {
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            Category = command.Category.Trim(),
            Priority = NormalizePriority(command.Priority),
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = command.CreatedByUserId,
            AssignedToUserId = null
        };

        var history = CreateAudit(
            ticketId: 0, // the repository sets the real id when it saves
            action: "Created",
            performedByUserId: command.CreatedByUserId,
            oldValue: null,
            newValue: JsonSerializer.Serialize(new
            {
                ticket.Title,
                ticket.Description,
                ticket.Category,
                ticket.Priority,
                ticket.Status,
                ticket.CreatedByUserId
            }),
            description: "Ticket created.");

        var createdTicket = await _ticketRepository.AddWithHistoryAsync(
            ticket, history, ct);

        return createdTicket.Id;
    }

    #endregion

    #region Helpers

    // NOTE: CreateAudit, NormalizePriority and the Validate* methods are
    // copied from TicketService. When you convert the Update handler you
    // will need them again. At that point move them into one shared
    // static class (for example TicketRules) and delete the copies.

    private static TicketHistory CreateAudit(
        int ticketId,
        string action,
        string performedByUserId,
        string? oldValue,
        string? newValue,
        string description)
    {
        return new TicketHistory
        {
            TicketId = ticketId,
            Action = action,
            PerformedByUserId = performedByUserId,
            OldValue = oldValue,
            NewValue = newValue,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void ValidateUserId(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "Authenticated user ID is required.",
                nameof(userId));
        }
    }

    private static void ValidateRequiredText(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{propertyName} is required.",
                propertyName);
        }
    }

    private static string NormalizePriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
        {
            throw new ArgumentException("Priority is required.");
        }

        var normalized = AllowedPriorities.FirstOrDefault(
            value => string.Equals(
                value,
                priority.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (normalized is null)
        {
            throw new ArgumentException(
                "Priority must be Low, Medium, High, or Critical.",
                nameof(priority));
        }

        return normalized;
    }

    #endregion
}