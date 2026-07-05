using Tiketin.Web.Domain;

namespace Tiketin.Web.Services.Interfaces;

/// <summary>
/// Ticket lifecycle notifications. Implementations must never throw:
/// a failed notification must not fail the business operation.
/// </summary>
public interface INotificationService
{
    Task TicketAssignedAsync(Ticket ticket, AppUser assignee, CancellationToken ct = default);

    Task TicketResolvedAsync(Ticket ticket, AppUser reporter, CancellationToken ct = default);
}
