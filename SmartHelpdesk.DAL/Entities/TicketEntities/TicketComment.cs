
using SmartHelpdesk.DAL.Entities.Ticket;

namespace SmartHelpdesk.DAL.Entities;

#region Entity

public class TicketComment
{
    #region Properties

    public int Id { get; set; }

    public int TicketId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string CommentText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    #endregion

    #region Navigation Properties

    public SmartHelpdesk.DAL.Entities.Ticket.Ticket? Ticket { get; set; }

    public ApplicationUser? User { get; set; }

    #endregion
}

#endregion
