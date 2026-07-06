using FluentAssertions;
using Tiketin.Web.Contracts;
using Tiketin.Web.Services;

namespace Tiketin.Tests.Unit;

public class SlaServiceTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);

    // Category targets used throughout: respond in 60 minutes, resolve in 240.
    private const int ResponseMinutes = 60;
    private const int ResolutionMinutes = 240;

    private static SlaService ServiceAt(DateTimeOffset now) => new(new FixedTimeProvider(now));

    [Fact]
    public void Response_met_when_first_response_is_before_the_deadline()
    {
        var result = ServiceAt(CreatedAt.AddHours(5)).Compute(
            CreatedAt, CreatedAt.AddMinutes(45), null, ResponseMinutes, ResolutionMinutes);

        result.Response.Result.Should().Be(SlaResult.Met);
        result.Response.IsBreached.Should().BeFalse();
    }

    [Fact]
    public void Response_breached_when_first_response_came_after_the_deadline()
    {
        var result = ServiceAt(CreatedAt.AddHours(5)).Compute(
            CreatedAt, CreatedAt.AddMinutes(61), null, ResponseMinutes, ResolutionMinutes);

        result.Response.Result.Should().Be(SlaResult.Breached);
        result.Response.IsBreached.Should().BeTrue();
    }

    [Fact]
    public void Response_exactly_at_the_deadline_still_counts_as_met()
    {
        var result = ServiceAt(CreatedAt.AddHours(5)).Compute(
            CreatedAt, CreatedAt.AddMinutes(ResponseMinutes), null, ResponseMinutes, ResolutionMinutes);

        result.Response.Result.Should().Be(SlaResult.Met);
    }

    [Fact]
    public void Response_pending_while_no_response_and_deadline_not_reached()
    {
        var result = ServiceAt(CreatedAt.AddMinutes(30)).Compute(
            CreatedAt, null, null, ResponseMinutes, ResolutionMinutes);

        result.Response.Result.Should().Be(SlaResult.Pending);
        result.Response.IsBreached.Should().BeFalse();
        result.Response.Deadline.Should().Be(CreatedAt.AddMinutes(ResponseMinutes));
    }

    [Fact]
    public void Response_pending_breached_when_deadline_passed_without_response()
    {
        var result = ServiceAt(CreatedAt.AddMinutes(90)).Compute(
            CreatedAt, null, null, ResponseMinutes, ResolutionMinutes);

        result.Response.Result.Should().Be(SlaResult.PendingBreached);
        result.Response.IsBreached.Should().BeTrue();
    }

    [Fact]
    public void Resolution_met_when_resolved_within_target()
    {
        var result = ServiceAt(CreatedAt.AddDays(2)).Compute(
            CreatedAt, CreatedAt.AddMinutes(30), CreatedAt.AddMinutes(200),
            ResponseMinutes, ResolutionMinutes);

        result.Resolution.Result.Should().Be(SlaResult.Met);
    }

    [Fact]
    public void Resolution_breached_when_resolved_after_target()
    {
        var result = ServiceAt(CreatedAt.AddDays(2)).Compute(
            CreatedAt, CreatedAt.AddMinutes(30), CreatedAt.AddMinutes(500),
            ResponseMinutes, ResolutionMinutes);

        result.Resolution.Result.Should().Be(SlaResult.Breached);
    }

    [Fact]
    public void Resolution_keeps_running_independently_after_first_response()
    {
        // Responded on time, but not resolved yet and the resolution window passed.
        var result = ServiceAt(CreatedAt.AddMinutes(300)).Compute(
            CreatedAt, CreatedAt.AddMinutes(10), null, ResponseMinutes, ResolutionMinutes);

        result.Response.Result.Should().Be(SlaResult.Met);
        result.Resolution.Result.Should().Be(SlaResult.PendingBreached);
    }
}
