
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHelpdesk.BLL.Interfaces;
using SmartHelpdesk.Common.DTOs.AI;
using System.Net.Http;
using System.Text.Json;

namespace SmartHelpdesk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AuthenticatedUser")]
public class AIController : ControllerBase
{
    #region Fields

    private readonly IAiTicketService _aiTicketService;
    private readonly ILogger<AIController> _logger;

    #endregion

    #region Ctor

    public AIController(
        IAiTicketService aiTicketService,
        ILogger<AIController> logger)
    {
        _aiTicketService = aiTicketService;
        _logger = logger;
    }

    #endregion

    #region Ticket Classification

    [HttpPost("tickets/classify")]
    [ProducesResponseType(typeof(AITicketClassificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<AITicketClassificationResponseDto>> ClassifyTicket(
        [FromBody] AITicketClassificationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _aiTicketService.ClassifyTicketAsync(
                request, cancellationToken);

            return Ok(result);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // The client cancelled the request.
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(
                ex, "AI ticket classification timed out.");

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The AI service timed out. Please try again." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex, "Unable to connect to the local AI service.");

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "The local AI service is unavailable. Ensure Ollama is running." });
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex, "The AI service returned an invalid response.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "The AI service returned an invalid response." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex, "AI classification response could not be validated.");

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "The AI service returned an unusable classification. Please retry." });
        }
    }

    #endregion
}
