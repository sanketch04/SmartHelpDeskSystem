using SmartHelpdesk.BLL.Common.CQRS;

namespace SmartHelpdesk.BLL.Features.Tickets.Commands.CreateTicket;

/// <summary>
/// Request to create a ticket. Returns the new ticket's Id.
/// CreatedByUserId comes from the JWT claims (set by the controller),
/// never from the request body.
/// </summary>
public record CreateTicketCommand(
    string Title,
    string Description,
    string Category,
    string Priority,
    string CreatedByUserId) : ICommand<int>;