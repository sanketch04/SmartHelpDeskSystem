
using SmartHelpdesk.DAL.Entities.Ticket;

namespace SmartHelpdesk.DAL.Entities;

#region Entity

public class TicketStatusHistory
{
    #region Properties

    public int Id { get; set; }

    public int TicketId { get; set; }

    public string OldStatus { get; set; } = string.Empty;

    public string NewStatus { get; set; } = string.Empty;

    public string ChangedByUserId { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    #endregion

    #region Navigation Properties

    public SmartHelpdesk.DAL.Entities.Ticket.Ticket? Ticket { get; set; }

    public ApplicationUser? ChangedByUser { get; set; }

    #endregion
}

#endregion
