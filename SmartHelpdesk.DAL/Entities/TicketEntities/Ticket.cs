namespace SmartHelpdesk.DAL.Entities.Ticket;

#region Entity
public class Ticket
{
    #region Properties
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public string Priority { get; set; } = "Medium";

    public string Status { get; set; } = "Open";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key: user who created the ticket
    public string CreatedByUserId { get; set; } = string.Empty;

    // Navigation property
    public ApplicationUser? CreatedByUser { get; set; }

    // Foreign key: assigned support agent
    public string? AssignedToUserId { get; set; }

    // Navigation property
    public ApplicationUser? AssignedToUser { get; set; }


    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();

    public ICollection<TicketStatusHistory> StatusHistory { get; set; }= new List<TicketStatusHistory>();

    #endregion
}
#endregion