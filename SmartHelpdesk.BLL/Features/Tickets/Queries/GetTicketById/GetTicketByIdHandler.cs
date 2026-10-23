using SmartHelpdesk.BLL.Common.CQRS;
using SmartHelpdesk.BLL.Features.Tickets.Common;
using SmartHelpdesk.Common.DTOs.TicketDtos;
using SmartHelpdesk.Common.DTOs.Tickets;
using SmartHelpdesk.DAL.Interfaces;

namespace SmartHelpdesk.BLL.Features.Tickets.Queries.GetTicketById;

public sealed class GetTicketByIdHandler
    : IQueryHandler<GetTicketByIdQuery, TicketResponseDto?>
{
    private readonly ITicketRepository _ticketRepository;

    public GetTicketByIdHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository
            ?? throw new ArgumentNullException(nameof(ticketRepository));
    }

    public async Task<TicketResponseDto?> HandleAsync(
        GetTicketByIdQuery query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.Id), "Ticket ID must be greater than zero.");
        }

        var ticket = await _ticketRepository.GetByIdAsync(query.Id, ct);

        // Read only: no Add, no Update, no SaveChanges anywhere in a query handler.
        return ticket?.ToResponse();
    }
}