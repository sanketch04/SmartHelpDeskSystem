using Microsoft.AspNetCore.Identity;
using SmartHelpdesk.BLL.Interfaces;
using SmartHelpdesk.Common.DTOs.TicketDtos;
using SmartHelpdesk.Common.DTOs.Tickets;
using SmartHelpdesk.DAL.Entities;
using SmartHelpdesk.DAL.Entities.Ticket;
using SmartHelpdesk.DAL.Interfaces;
using SmartHelpdesk.DAL.Repository;
using System.Text.Json;

namespace SmartHelpdesk.BLL.Implementation;

#region Class

public sealed class TicketService : ITicketService
{
    #region Fields

    private readonly ITicketRepository _ticketRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITicketHistoryRepository _ticketHistoryRepository;

    private static readonly HashSet<string> AllowedPriorities =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Low", "Medium", "High", "Critical"
        };

    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Open", "In Progress", "Pending", "Resolved", "Closed"
        };

    #endregion

    #region Ctor

    public TicketService(
        ITicketRepository ticketRepository,
        UserManager<ApplicationUser> userManager,
        ITicketHistoryRepository ticketHistoryRepository)
    {
        _ticketRepository = ticketRepository
            ?? throw new ArgumentNullException(nameof(ticketRepository));

        _userManager = userManager
            ?? throw new ArgumentNullException(nameof(userManager));

        _ticketHistoryRepository = ticketHistoryRepository
            ?? throw new ArgumentNullException(nameof(ticketHistoryRepository));
    }

    #endregion

    #region Get All Tickets

    public async Task<IReadOnlyList<TicketResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tickets = await _ticketRepository.GetAllAsync(cancellationToken);
        return tickets.Select(MapToResponse).ToList();
    }

    #endregion

    #region Get Ticket By Id

    public async Task<TicketResponseDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        var ticket = await _ticketRepository.GetByIdAsync(
            id, cancellationToken);

        return ticket is null ? null : MapToResponse(ticket);
    }

    #endregion

    #region Get My Tickets

    public async Task<IReadOnlyList<TicketResponseDto>> GetMyTicketsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        var tickets = await _ticketRepository.GetByCreatorUserIdAsync(
            userId, cancellationToken);

        return tickets.Select(MapToResponse).ToList();
    }

    #endregion

    #region Get Assigned Tickets

    public async Task<IReadOnlyList<TicketResponseDto>> GetAssignedTicketsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        var tickets = await _ticketRepository.GetAssignedToUserIdAsync(
            userId, cancellationToken);

        return tickets.Select(MapToResponse).ToList();
    }

    #endregion

    #region Create Ticket

    public async Task<TicketResponseDto> CreateAsync(
        CreateTicketDto dto,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateUserId(currentUserId);

        var creator = await _userManager.FindByIdAsync(currentUserId);

        if (creator is null || !creator.IsActive)
        {
            throw new InvalidOperationException(
                "The authenticated user does not exist or is inactive.");
        }

        ValidateRequiredText(dto.Title, nameof(dto.Title));
        ValidateRequiredText(dto.Description, nameof(dto.Description));
        ValidateRequiredText(dto.Category, nameof(dto.Category));

        var ticket = new Ticket
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Category = dto.Category.Trim(),
            Priority = NormalizePriority(dto.Priority),
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUserId,
            AssignedToUserId = null
        };

        var history = CreateAudit(
            ticketId: 0,
            action: "Created",
            performedByUserId: currentUserId,
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
            ticket, history, cancellationToken);

        return MapToResponse(createdTicket);
    }

    #endregion

    #region Update Ticket

    public async Task<TicketResponseDto?> UpdateAsync(
        int id,
        UpdateTicketDto dto,
        string updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateId(id);
        ValidateUserId(updatedByUserId);

        ValidateRequiredText(dto.Title, nameof(dto.Title));
        ValidateRequiredText(dto.Description, nameof(dto.Description));
        ValidateRequiredText(dto.Category, nameof(dto.Category));

        var actor = await _userManager.FindByIdAsync(updatedByUserId);

        if (actor is null || !actor.IsActive)
        {
            throw new InvalidOperationException(
                "The authenticated user does not exist or is inactive.");
        }

        var ticket = await _ticketRepository.GetByIdAsync(
            id, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var newTitle = dto.Title.Trim();
        var newDescription = dto.Description.Trim();
        var newCategory = dto.Category.Trim();
        var newPriority = NormalizePriority(dto.Priority);

        var oldValues = new Dictionary<string, string?>();
        var newValues = new Dictionary<string, string?>();

        AddChange(oldValues, newValues, "Title",
            ticket.Title, newTitle);

        AddChange(oldValues, newValues, "Description",
            ticket.Description, newDescription);

        AddChange(oldValues, newValues, "Category",
            ticket.Category, newCategory);

        AddChange(oldValues, newValues, "Priority",
            ticket.Priority, newPriority);

        if (oldValues.Count == 0)
        {
            return MapToResponse(ticket);
        }

        var history = CreateAudit(
            ticket.Id,
            "Updated",
            updatedByUserId,
            JsonSerializer.Serialize(oldValues),
            JsonSerializer.Serialize(newValues),
            "Ticket details updated.");

        ticket.Title = newTitle;
        ticket.Description = newDescription;
        ticket.Category = newCategory;
        ticket.Priority = newPriority;

        await _ticketRepository.UpdateWithHistoryAsync(
            ticket, history, cancellationToken);

        return MapToResponse(ticket);
    }

    #endregion

    #region Update Ticket Status

    public async Task<TicketResponseDto?> UpdateStatusAsync(
        int id,
        string status,
        string changedByUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        ValidateUserId(changedByUserId);
        ValidateRequiredText(status, nameof(status));

        var normalizedStatus = AllowedStatuses.FirstOrDefault(
            value => string.Equals(
                value,
                status.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (normalizedStatus is null)
        {
            throw new ArgumentException(
                "Status must be Open, In Progress, Pending, Resolved, or Closed.",
                nameof(status));
        }

        var actor = await _userManager.FindByIdAsync(changedByUserId);

        if (actor is null || !actor.IsActive)
        {
            throw new InvalidOperationException(
                "The authenticated user does not exist or is inactive.");
        }

        var ticket = await _ticketRepository.GetByIdAsync(
            id, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        if (string.Equals(
            ticket.Status,
            normalizedStatus,
            StringComparison.OrdinalIgnoreCase))
        {
            return MapToResponse(ticket);
        }

        var oldStatus = ticket.Status;
        var changedAt = DateTime.UtcNow;

        var statusHistory = new TicketStatusHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = normalizedStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = changedAt
        };

        var auditHistory = CreateAudit(
            ticket.Id,
            "StatusChanged",
            changedByUserId,
            oldStatus,
            normalizedStatus,
            $"Ticket status changed from '{oldStatus}' to '{normalizedStatus}'.");

        ticket.Status = normalizedStatus;

        await _ticketRepository.UpdateStatusWithHistoryAsync(
            ticket,
            statusHistory,
            auditHistory,
            cancellationToken);

        return MapToResponse(ticket);
    }

    #endregion

    #region Assign Ticket

    public async Task<TicketResponseDto?> AssignAsync(
        int ticketId,
        string assignedToUserId,
        string assignedByUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(ticketId);
        ValidateUserId(assignedToUserId);
        ValidateUserId(assignedByUserId);

        var actor = await _userManager.FindByIdAsync(assignedByUserId);

        if (actor is null || !actor.IsActive)
        {
            throw new InvalidOperationException(
                "The authenticated user does not exist or is inactive.");
        }

        var ticket = await _ticketRepository.GetByIdAsync(
            ticketId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var agent = await _userManager.FindByIdAsync(assignedToUserId);

        if (agent is null || !agent.IsActive)
        {
            throw new InvalidOperationException(
                "The selected support agent does not exist or is inactive.");
        }

        var roles = await _userManager.GetRolesAsync(agent);

        if (!roles.Contains("SupportAgent", StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "The selected user must have the SupportAgent role.");
        }

        var previousAssigneeId = ticket.AssignedToUserId;

        if (string.Equals(
            previousAssigneeId,
            agent.Id,
            StringComparison.Ordinal))
        {
            return MapToResponse(ticket);
        }

        var history = CreateAudit(
            ticket.Id,
            previousAssigneeId is null ? "Assigned" : "Reassigned",
            assignedByUserId,
            previousAssigneeId,
            agent.Id,
            previousAssigneeId is null
                ? $"Ticket assigned to support agent '{agent.FullName}'."
                : $"Ticket reassigned from '{previousAssigneeId}' to '{agent.Id}'.");

        ticket.AssignedToUserId = agent.Id;

        await _ticketRepository.UpdateWithHistoryAsync(
            ticket, history, cancellationToken);

        return MapToResponse(ticket);
    }

    #endregion

    #region Add Ticket Comment

    public async Task<TicketCommentResponseDto?> AddCommentAsync(
        int ticketId,
        string userId,
        CreateTicketCommentDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateId(ticketId);
        ValidateUserId(userId);
        ArgumentNullException.ThrowIfNull(dto);

        ValidateRequiredText(dto.CommentText, nameof(dto.CommentText));

        var commentText = dto.CommentText.Trim();

        if (commentText.Length > 4000)
        {
            throw new ArgumentException(
                "Comment cannot exceed 4000 characters.",
                nameof(dto.CommentText));
        }

        var ticket = await _ticketRepository.GetByIdAsync(
            ticketId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException(
                "The authenticated user does not exist or is inactive.");
        }

        var now = DateTime.UtcNow;

        var comment = new TicketComment
        {
            TicketId = ticketId,
            UserId = userId,
            CommentText = commentText,
            CreatedAt = now
        };

        var history = CreateAudit(
            ticketId,
            "CommentAdded",
            userId,
            null,
            "Comment added",
            "A ticket comment was added.");

        var savedComment =
            await _ticketRepository.AddCommentWithHistoryAsync(
                comment, history, cancellationToken);

        return new TicketCommentResponseDto
        {
            Id = savedComment.Id,
            TicketId = savedComment.TicketId,
            UserId = savedComment.UserId,
            UserName = user.FullName,
            CommentText = savedComment.CommentText,
            CreatedAt = savedComment.CreatedAt
        };
    }

    #endregion

    #region Get Ticket Comments

    public async Task<IReadOnlyList<TicketCommentResponseDto>?> GetCommentsAsync(
        int ticketId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(ticketId);

        var ticket = await _ticketRepository.GetByIdAsync(
            ticketId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var comments = await _ticketRepository.GetCommentsByTicketIdAsync(
            ticketId, cancellationToken);

        return comments.Select(comment => new TicketCommentResponseDto
        {
            Id = comment.Id,
            TicketId = comment.TicketId,
            UserId = comment.UserId,
            UserName = comment.User?.FullName,
            CommentText = comment.CommentText,
            CreatedAt = comment.CreatedAt
        }).ToList();
    }

    #endregion

    #region Get Ticket Status History

    public async Task<IReadOnlyList<TicketStatusHistoryResponseDto>?>
        GetStatusHistoryAsync(
            int ticketId,
            CancellationToken cancellationToken = default)
    {
        ValidateId(ticketId);

        var ticket = await _ticketRepository.GetByIdAsync(
            ticketId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var history =
            await _ticketRepository.GetStatusHistoryByTicketIdAsync(
                ticketId, cancellationToken);

        return history.Select(item => new TicketStatusHistoryResponseDto
        {
            Id = item.Id,
            TicketId = item.TicketId,
            OldStatus = item.OldStatus,
            NewStatus = item.NewStatus,
            ChangedByUserId = item.ChangedByUserId,
            ChangedByUserName = item.ChangedByUser?.FullName,
            ChangedAt = item.ChangedAt
        }).ToList();
    }

    #endregion

    #region Delete Ticket

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        var ticket = await _ticketRepository.GetByIdAsync(
            id, cancellationToken);

        if (ticket is null)
        {
            return false;
        }

        // Audit history is intentionally preserved.
        // Configure deletion as soft delete or implement an audited
        // transactional delete before allowing deletion of audited tickets.
        throw new InvalidOperationException(
            "Ticket deletion is temporarily disabled to preserve audit history.");
    }

    #endregion

    #region Audit Helpers

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

    private static void AddChange(
        IDictionary<string, string?> oldValues,
        IDictionary<string, string?> newValues,
        string fieldName,
        string? oldValue,
        string? newValue)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return;
        }

        oldValues[fieldName] = oldValue;
        newValues[fieldName] = newValue;
    }

    #endregion

    #region Validation

    private static void ValidateId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id), "Ticket ID must be greater than zero.");
        }
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

    private static void ValidateRequiredText(
        string? value,
        string propertyName)
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

    #region Mapping

    private static TicketResponseDto MapToResponse(Ticket ticket)
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

    #region Get Ticket Audit History

    public async Task<IReadOnlyList<TicketHistoryResponseDto>?>
        GetAuditHistoryAsync(
            int ticketId,
            CancellationToken cancellationToken = default)
    {
        ValidateId(ticketId);

        var ticket = await _ticketRepository.GetByIdAsync(
            ticketId,
            cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var history = await _ticketHistoryRepository.GetByTicketIdAsync(
            ticketId,
            cancellationToken);

        return history.Select(item => new TicketHistoryResponseDto
        {
            Id = item.Id,
            TicketId = item.TicketId,
            Action = item.Action,
            PerformedByUserId = item.PerformedByUserId,
            PerformedByUserName = item.PerformedByUser?.FullName,
            OldValue = item.OldValue,
            NewValue = item.NewValue,
            Description = item.Description,
            CreatedAt = item.CreatedAt
        }).ToList();
    }

    #endregion

    #endregion
}

#endregion