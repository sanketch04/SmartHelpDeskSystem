using SmartHelpdesk.Common.DTOs.TicketDtos;
using SmartHelpdesk.Common.DTOs.Tickets;
using SmartHelpdesk.DAL.Entities.Ticket;

namespace SmartHelpdesk.BLL.Features.Tickets.Common;

/// <summary>
/// One shared place for entity -> DTO mapping, so every query handler
/// (get by id, get all, my tickets, assigned tickets) uses the same code
/// instead of each keeping its own copy of MapToResponse.
/// </summary>
public static class TicketMappings
{
    public static TicketResponseDto ToResponse(this Ticket ticket)
    {
        return new TicketResponseDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Category = ticket.Category,
            Priority = ticket.Priority,
            Status = ticket.Status,
            CreatedAt = ticket.CreatedAt,
            CreatedByUserId = ticket.CreatedByUserId,
            AssignedToUserId = ticket.AssignedToUserId
        };
    }
}