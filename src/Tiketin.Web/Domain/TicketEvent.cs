namespace Tiketin.Web.Domain;

/// <summary>Audit-trail entry for a ticket (activity log).</summary>
public class TicketEvent
{
    public long Id { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    /// <summary>Null when the event was produced by the system (e.g. auto-close job).</summary>
    public Guid? ActorId { get; set; }
    public AppUser? Actor { get; set; }

    public TicketEventType EventType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
