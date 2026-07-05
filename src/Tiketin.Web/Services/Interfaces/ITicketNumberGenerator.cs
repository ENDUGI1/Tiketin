namespace Tiketin.Web.Services.Interfaces;

public interface ITicketNumberGenerator
{
    /// <summary>
    /// Returns the next ticket number for the month of <paramref name="now"/>,
    /// format TKT-YYYYMM-0001. Must be safe under concurrency.
    /// </summary>
    Task<string> NextAsync(DateTimeOffset now, CancellationToken ct = default);
}
