using Tiketin.Web.Domain;

namespace Tiketin.Web.Services;

/// <summary>
/// Single source of truth for legal ticket status transitions:
///   Open       -> InProgress, Resolved
///   InProgress -> Resolved, Open
///   Resolved   -> Closed, Reopened
///   Reopened   -> InProgress, Resolved
///   Closed     -> (terminal)
/// </summary>
public static class TicketStatusTransitionValidator
{
    private static readonly IReadOnlyDictionary<TicketStatus, TicketStatus[]> Allowed =
        new Dictionary<TicketStatus, TicketStatus[]>
        {
            [TicketStatus.Open] = [TicketStatus.InProgress, TicketStatus.Resolved],
            [TicketStatus.InProgress] = [TicketStatus.Resolved, TicketStatus.Open],
            [TicketStatus.Resolved] = [TicketStatus.Closed, TicketStatus.Reopened],
            [TicketStatus.Reopened] = [TicketStatus.InProgress, TicketStatus.Resolved],
            [TicketStatus.Closed] = []
        };

    public static bool IsAllowed(TicketStatus from, TicketStatus to)
        => Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyList<TicketStatus> AllowedFrom(TicketStatus from)
        => Allowed.TryGetValue(from, out var targets) ? targets : [];
}
