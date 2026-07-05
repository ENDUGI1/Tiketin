using System.ComponentModel.DataAnnotations;
using Tiketin.Web.Domain;

namespace Tiketin.Web.Contracts;

// ---------- Requests ----------

public record CreateTicketRequest(
    [Required, MaxLength(150)] string Title,
    [Required] string Description,
    [Required, Range(1, int.MaxValue)] int CategoryId,
    [Required, EnumDataType(typeof(TicketPriority))] TicketPriority Priority);

public record AddCommentRequest(
    [Required] string Body,
    bool IsInternal = false);

public record RateTicketRequest(
    [Required, Range(1, 5)] short Rating,
    [MaxLength(300)] string? Note);

/// <summary>List filters; all optional. Bound from the query string.</summary>
public class TicketListQuery
{
    public TicketStatus? Status { get; init; }
    public int? Category { get; init; }
    public TicketPriority? Priority { get; init; }
    public Guid? Assignee { get; init; }

    /// <summary>Matches ticket number, title, or description (case-insensitive).</summary>
    public string? Search { get; init; }

    /// <summary>When true and no explicit status filter, hides Resolved and Closed tickets.</summary>
    public bool ActiveOnly { get; init; }

    /// <summary>Queue ordering: priority first, then oldest. Default is newest first.</summary>
    public bool QueueOrder { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public record ChangeStatusRequest(
    [Required, EnumDataType(typeof(TicketStatus))] TicketStatus Status);

/// <summary>Null assignee unassigns the ticket (admin only).</summary>
public record AssignTicketRequest(Guid? AssigneeId);

public record ChangePriorityRequest(
    [Required, EnumDataType(typeof(TicketPriority))] TicketPriority Priority);

// ---------- Responses ----------

public record TechnicianResponse(Guid Id, string FullName);

public record TicketListItem(
    Guid Id,
    string TicketNumber,
    string Title,
    string CategoryName,
    int SlaResponseMinutes,
    int SlaResolutionMinutes,
    TicketPriority Priority,
    TicketStatus Status,
    string ReporterName,
    string? AssigneeName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FirstResponseAt,
    DateTimeOffset? ResolvedAt);

public record TicketDetailResponse(
    Guid Id,
    string TicketNumber,
    string Title,
    string Description,
    int CategoryId,
    string CategoryName,
    int SlaResponseMinutes,
    int SlaResolutionMinutes,
    TicketPriority Priority,
    TicketStatus Status,
    Guid ReporterId,
    string ReporterName,
    string ReporterDepartment,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTimeOffset? FirstResponseAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    short? SatisfactionRating,
    string? SatisfactionNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<CommentResponse> Comments,
    IReadOnlyList<AttachmentResponse> Attachments);

public record CommentResponse(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Body,
    bool IsInternal,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AttachmentResponse> Attachments);

public record AttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string UploadedByName,
    DateTimeOffset CreatedAt);

public record TicketEventResponse(
    long Id,
    TicketEventType EventType,
    string? ActorName,
    string? OldValue,
    string? NewValue,
    DateTimeOffset CreatedAt);

public record CategoryResponse(
    int Id,
    string Name,
    int SlaResponseMinutes,
    int SlaResolutionMinutes);
