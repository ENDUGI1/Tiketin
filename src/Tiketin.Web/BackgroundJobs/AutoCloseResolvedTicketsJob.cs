using Microsoft.EntityFrameworkCore;
using Tiketin.Web.Data;
using Tiketin.Web.Domain;

namespace Tiketin.Web.BackgroundJobs;

/// <summary>
/// Closes tickets that have stayed Resolved for more than 7 days without the
/// reporter reopening them. Runs hourly.
/// </summary>
public class AutoCloseResolvedTicketsJob(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<AutoCloseResolvedTicketsJob> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    public static readonly TimeSpan CloseAfter = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        // First pass shortly after startup, then hourly.
        do
        {
            try
            {
                await CloseExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-close pass failed; retrying next interval");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CloseExpiredAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = clock.GetUtcNow();
        var threshold = now - CloseAfter;

        var expired = await db.Tickets
            .Where(t => t.Status == TicketStatus.Resolved && t.ResolvedAt != null && t.ResolvedAt < threshold)
            .ToListAsync(ct);

        if (expired.Count == 0)
        {
            return;
        }

        foreach (var ticket in expired)
        {
            ticket.Status = TicketStatus.Closed;
            ticket.ClosedAt = now;
            ticket.UpdatedAt = now;

            db.TicketEvents.Add(new TicketEvent
            {
                TicketId = ticket.Id,
                ActorId = null, // system action
                EventType = TicketEventType.StatusChanged,
                OldValue = TicketStatus.Resolved.ToString(),
                NewValue = TicketStatus.Closed.ToString(),
                CreatedAt = now
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Auto-closed {Count} resolved ticket(s)", expired.Count);
    }
}
