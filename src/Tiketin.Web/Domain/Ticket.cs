namespace Tiketin.Web.Domain;

public class Ticket
{
    public Guid Id { get; set; }

    /// <summary>Human-readable number, format TKT-YYYYMM-0001, sequential per month.</summary>
    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public Guid ReporterId { get; set; }
    public AppUser Reporter { get; set; } = null!;

    public Guid? AssigneeId { get; set; }
    public AppUser? Assignee { get; set; }

    public DateTimeOffset? FirstResponseAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>1..5, set once by the reporter after resolution.</summary>
    public short? SatisfactionRating { get; set; }
    public string? SatisfactionNote { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketEvent> Events { get; set; } = new List<TicketEvent>();
}
