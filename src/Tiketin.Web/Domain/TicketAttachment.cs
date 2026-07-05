namespace Tiketin.Web.Domain;

public class TicketAttachment
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public Guid? CommentId { get; set; }
    public TicketComment? Comment { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;

    public Guid UploadedById { get; set; }
    public AppUser UploadedBy { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
