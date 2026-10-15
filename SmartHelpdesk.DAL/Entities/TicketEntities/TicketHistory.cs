using System.ComponentModel.DataAnnotations;
using SmartHelpdesk.DAL.Entities.Ticket;

namespace SmartHelpdesk.DAL.Entities;

#region Entity
public sealed class TicketHistory
{
    #region Properties

    public int Id { get; set; }

    [Required]
    public int TicketId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required]
    public string PerformedByUserId { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? OldValue { get; set; }

    [MaxLength(2000)]
    public string? NewValue { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    #endregion

    #region Navigation Properties

    public SmartHelpdesk.DAL.Entities.Ticket.Ticket? Ticket { get; set; }

    public ApplicationUser? PerformedByUser { get; set; }

    #endregion
}
#endregion