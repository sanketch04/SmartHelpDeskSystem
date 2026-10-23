using SmartHelpdesk.BLL.Common.CQRS;
using SmartHelpdesk.Common.DTOs.TicketDtos;
using SmartHelpdesk.Common.DTOs.Tickets;

namespace SmartHelpdesk.BLL.Features.Tickets.Queries.GetTicketById;

/// <summary>
/// Request to read one ticket. Returns null when the ticket does not exist.
/// A query never changes data.
/// </summary>
public record GetTicketByIdQuery(int Id) : IQuery<TicketResponseDto?>;

// NOTE: TicketService imports both DTO namespaces above. Keep only the one
// that actually contains TicketResponseDto and delete the other using.