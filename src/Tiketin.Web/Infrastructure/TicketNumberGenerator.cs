using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tiketin.Web.Data;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Infrastructure;

/// <summary>
/// Generates ticket numbers from a Postgres sequence per month. `nextval` is atomic,
/// so concurrent creates never collide and there is no row locking involved.
/// </summary>
public class TicketNumberGenerator(AppDbContext db) : ITicketNumberGenerator
{
    public const string Prefix = "TKT";

    public async Task<string> NextAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var yearMonth = now.UtcDateTime.ToString("yyyyMM");
        var sequenceName = $"ticket_seq_{yearMonth}";

        // EF1002: identifiers cannot be parameterized; sequenceName is derived from
        // a date format string, never from user input.
#pragma warning disable EF1002
        try
        {
            // First ticket of a new month creates the sequence. IF NOT EXISTS still
            // throws under a concurrent create race (unique_violation on pg_class),
            // which is harmless: the sequence exists afterwards either way.
            await db.Database.ExecuteSqlRawAsync(
                $"CREATE SEQUENCE IF NOT EXISTS {sequenceName} START 1", ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation
                                            || ex.SqlState == PostgresErrorCodes.DuplicateTable)
        {
        }

        var next = await db.Database
            .SqlQueryRaw<long>($"SELECT nextval('{sequenceName}') AS \"Value\"")
            .SingleAsync(ct);
#pragma warning restore EF1002

        return Format(yearMonth, next);
    }

    public static string Format(string yearMonth, long sequenceValue)
        => $"{Prefix}-{yearMonth}-{sequenceValue:D4}";
}
