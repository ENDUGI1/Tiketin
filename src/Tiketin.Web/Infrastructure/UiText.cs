using Tiketin.Web.Domain;

namespace Tiketin.Web.Infrastructure;

/// <summary>Indonesian display labels for domain enums (UI language is Indonesian).</summary>
public static class UiText
{
    public static string StatusLabel(TicketStatus status) => status switch
    {
        TicketStatus.Open => "Terbuka",
        TicketStatus.InProgress => "Dikerjakan",
        TicketStatus.Resolved => "Selesai",
        TicketStatus.Closed => "Ditutup",
        TicketStatus.Reopened => "Dibuka Ulang",
        _ => status.ToString()
    };

    public static string StatusCss(TicketStatus status) => status switch
    {
        TicketStatus.Open => "status--open",
        TicketStatus.InProgress => "status--inprogress",
        TicketStatus.Resolved => "status--resolved",
        TicketStatus.Closed => "status--closed",
        TicketStatus.Reopened => "status--reopened",
        _ => "status--open"
    };

    public static string PriorityLabel(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "Rendah",
        TicketPriority.Medium => "Sedang",
        TicketPriority.High => "Tinggi",
        TicketPriority.Critical => "Kritis",
        _ => priority.ToString()
    };

    public static string PriorityCss(TicketPriority priority) => priority switch
    {
        TicketPriority.Critical => "priority priority--critical",
        TicketPriority.High => "priority priority--high",
        _ => "priority"
    };

    public static string EventLabel(TicketEventType type) => type switch
    {
        TicketEventType.Created => "Tiket dibuat",
        TicketEventType.StatusChanged => "Status diubah",
        TicketEventType.Assigned => "Teknisi ditugaskan",
        TicketEventType.PriorityChanged => "Prioritas diubah",
        TicketEventType.Commented => "Komentar ditambahkan",
        TicketEventType.Reopened => "Tiket dibuka ulang",
        _ => type.ToString()
    };

    /// <summary>Compact local timestamp, monospace-friendly: 05 Jul 2026 14:03.</summary>
    public static string Timestamp(DateTimeOffset value)
        => value.ToLocalTime().ToString("dd MMM yyyy HH:mm");
}
