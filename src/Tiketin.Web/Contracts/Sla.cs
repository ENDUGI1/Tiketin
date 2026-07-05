namespace Tiketin.Web.Contracts;

public enum SlaResult
{
    /// <summary>Clock still running, deadline not reached.</summary>
    Pending,

    /// <summary>Clock still running past the deadline.</summary>
    PendingBreached,

    /// <summary>Completed within the deadline.</summary>
    Met,

    /// <summary>Completed after the deadline.</summary>
    Breached
}

public record SlaClock(SlaResult Result, DateTimeOffset Deadline)
{
    public bool IsBreached => Result is SlaResult.Breached or SlaResult.PendingBreached;
}

public record SlaComputation(SlaClock Response, SlaClock Resolution);
