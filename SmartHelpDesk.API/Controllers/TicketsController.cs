using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHelpdesk.BLL.Common.CQRS;
using SmartHelpdesk.BLL.Features.Tickets.Commands.CreateTicket;
using SmartHelpdesk.BLL.Features.Tickets.Queries.GetTicketById;
using SmartHelpdesk.BLL.Interfaces;
using SmartHelpdesk.Common.DTOs.TicketDtos;
using System.Security.Claims;

namespace SmartHelpdesk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    #region Fields

    private readonly ITicketService _ticketService;
    private readonly IAuthorizationService _authorizationService;

    // CQRS handlers
    private readonly ICommandHandler<CreateTicketCommand, int> _createTicket;
    private readonly IQueryHandler<GetTicketByIdQuery, TicketResponseDto?> _getTicketById;

    #endregion

    #region Ctor

    public TicketsController(
        ITicketService ticketService,
        IAuthorizationService authorizationService,
        ICommandHandler<CreateTicketCommand, int> createTicket,
        IQueryHandler<GetTicketByIdQuery, TicketResponseDto?> getTicketById)
    {
        _ticketService = ticketService
            ?? throw new ArgumentNullException(nameof(ticketService));

        _authorizationService = authorizationService
            ?? throw new ArgumentNullException(nameof(authorizationService));

        _createTicket = createTicket
            ?? throw new ArgumentNullException(nameof(createTicket));

        _getTicketById = getTicketById
            ?? throw new ArgumentNullException(nameof(getTicketById));
    }

    #endregion

    #region Helpers

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Administrator, the ticket's creator, the assigned agent, or anyone
    /// with the Tickets.ViewAll permission may view a ticket.
    /// Shared by GetById and GetAuditHistory so the rule exists once.
    /// </summary>
    private async Task<bool> CanViewTicketAsync(TicketResponseDto ticket, string currentUserId)
    {
        if (User.IsInRole("Administrator"))
        {
            return true;
        }

        if (ticket.CreatedByUserId == currentUserId || ticket.AssignedToUserId == currentUserId)
        {
            return true;
        }

        var authorizationResult = await _authorizationService.AuthorizeAsync(
            User,
            resource: null,
            policyName: "Permission:Tickets.ViewAll");

        return authorizationResult.Succeeded;
    }

    #endregion

    #region Get All Tickets

    [HttpGet]
    [Authorize(Policy = "AdministratorOnly")]
    public async Task<ActionResult<IReadOnlyList<TicketResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var tickets = await _ticketService.GetAllAsync(cancellationToken);

        return Ok(tickets);
    }

    #endregion

    #region Get My Tickets

    [HttpGet("my")]
    [Authorize(Policy = "Permission:Tickets.ViewOwn")]
    public async Task<ActionResult<IReadOnlyList<TicketResponseDto>>> GetMyTickets(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var tickets = await _ticketService.GetMyTicketsAsync(
            userId,
            cancellationToken);

        return Ok(tickets);
    }

    #endregion

    #region Get Assigned Tickets

    [HttpGet("assigned")]
    [Authorize(Policy = "Permission:Tickets.ViewAll")]
    public async Task<ActionResult<IReadOnlyList<TicketResponseDto>>> GetAssignedTickets(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var tickets = await _ticketService.GetAssignedTicketsAsync(
            userId,
            cancellationToken);

        return Ok(tickets);
    }

    #endregion

    #region Get Ticket By Id

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketResponseDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid ticket ID." });
        }

        var currentUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized();
        }

        var ticket = await _getTicketById.HandleAsync(
            new GetTicketByIdQuery(id),
            cancellationToken);

        if (ticket is null)
        {
            return NotFound(new { message = "Ticket not found." });
        }

        if (!await CanViewTicketAsync(ticket, currentUserId))
        {
            return Forbid();
        }

        return Ok(ticket);
    }

    #endregion

    #region Create Ticket

    [HttpPost]
    [Authorize(Policy = "Permission:Tickets.Create")]
    public async Task<ActionResult<TicketResponseDto>> Create(
        [FromBody] CreateTicketDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            // Command: changes data, returns only the new Id.
            var command = new CreateTicketCommand(
                dto.Title,
                dto.Description,
                dto.Category,
                dto.Priority,
                userId);

            var newId = await _createTicket.HandleAsync(
                command,
                cancellationToken);

            // Query: read the saved ticket back so the response body
            // stays the same as before the CQRS change.
            var created = await _getTicketById.HandleAsync(
                new GetTicketByIdQuery(newId),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = newId },
                created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    #endregion

    #region Update Ticket

    [HttpPut("{id:int}")]
    [Authorize(Policy = "SupportAgentOrAdministrator")]
    public async Task<ActionResult<TicketResponseDto>> Update(
        int id,
        [FromBody] UpdateTicketDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var ticket = await _ticketService.UpdateAsync(
                id,
                dto,
                userId,
                cancellationToken);

            if (ticket is null)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            return Ok(ticket);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    #endregion

    #region Delete Ticket

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdministratorOnly")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _ticketService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(new { message = "Ticket not found." });
        }

        return NoContent();
    }

    #endregion

    #region Assign Ticket

    [HttpPut("{id:int}/assign")]
    [Authorize(Policy = "AdministratorOnly")]
    public async Task<IActionResult> AssignTicket(
        int id,
        [FromBody] AssignTicketDto dto,
        CancellationToken cancellationToken)
    {
        if (dto is null ||
            string.IsNullOrWhiteSpace(dto.AssignedToUserId))
        {
            return BadRequest(new { message = "AssignedToUserId is required." });
        }

        var assignedByUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(assignedByUserId))
        {
            return Unauthorized();
        }

        try
        {
            var ticket = await _ticketService.AssignAsync(
                id,
                dto.AssignedToUserId,
                assignedByUserId,
                cancellationToken);

            if (ticket is null)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            return Ok(ticket);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    #endregion

    #region Update Ticket Status

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "Permission:Tickets.UpdateStatus")]
    public async Task<ActionResult<TicketResponseDto>> UpdateStatus(
        int id,
        [FromBody] UpdateTicketStatusDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var ticket = await _ticketService.UpdateStatusAsync(
                id,
                dto.Status,
                userId,
                cancellationToken);

            if (ticket is null)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            return Ok(ticket);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    #endregion

    #region Get Ticket Audit History

    [HttpGet("{id:int}/audit-history")]
    public async Task<IActionResult> GetAuditHistory(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid ticket ID." });
        }

        var currentUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized();
        }

        var ticket = await _getTicketById.HandleAsync(
            new GetTicketByIdQuery(id),
            cancellationToken);

        if (ticket is null)
        {
            return NotFound(new { message = "Ticket not found." });
        }

        if (!await CanViewTicketAsync(ticket, currentUserId))
        {
            return Forbid();
        }

        var history = await _ticketService.GetAuditHistoryAsync(
            id,
            cancellationToken);

        return Ok(history);
    }

    #endregion
}