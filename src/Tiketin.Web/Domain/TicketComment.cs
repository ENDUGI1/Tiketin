namespace Tiketin.Web.Domain;

public class TicketComment
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;

    public string Body { get; set; } = string.Empty;

    /// <summary>Internal notes are visible to technicians and admins only.</summary>
    public bool IsInternal { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}
