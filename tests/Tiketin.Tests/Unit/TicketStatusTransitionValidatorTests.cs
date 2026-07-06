using FluentAssertions;
using Tiketin.Web.Domain;
using Tiketin.Web.Services;

namespace Tiketin.Tests.Unit;

public class TicketStatusTransitionValidatorTests
{
    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Open, TicketStatus.Resolved)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Open)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Reopened)]
    [InlineData(TicketStatus.Reopened, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Reopened, TicketStatus.Resolved)]
    public void Legal_transitions_are_allowed(TicketStatus from, TicketStatus to)
    {
        TicketStatusTransitionValidator.IsAllowed(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.Closed)]
    [InlineData(TicketStatus.Open, TicketStatus.Reopened)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Closed)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Reopened)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Open)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Reopened, TicketStatus.Open)]
    [InlineData(TicketStatus.Reopened, TicketStatus.Closed)]
    public void Illegal_transitions_are_rejected(TicketStatus from, TicketStatus to)
    {
        TicketStatusTransitionValidator.IsAllowed(from, to).Should().BeFalse();
    }

    [Theory]
    [InlineData(TicketStatus.Open)]
    [InlineData(TicketStatus.InProgress)]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Reopened)]
    [InlineData(TicketStatus.Closed)]
    public void Closed_is_terminal(TicketStatus target)
    {
        TicketStatusTransitionValidator.IsAllowed(TicketStatus.Closed, target).Should().BeFalse();
    }

    [Fact]
    public void No_status_can_transition_to_itself()
    {
        foreach (var status in Enum.GetValues<TicketStatus>())
        {
            TicketStatusTransitionValidator.IsAllowed(status, status)
                .Should().BeFalse($"{status} -> {status} must not be a transition");
        }
    }

    [Fact]
    public void AllowedFrom_matches_IsAllowed()
    {
        foreach (var from in Enum.GetValues<TicketStatus>())
        {
            var listed = TicketStatusTransitionValidator.AllowedFrom(from);
            foreach (var to in Enum.GetValues<TicketStatus>())
            {
                listed.Contains(to).Should().Be(
                    TicketStatusTransitionValidator.IsAllowed(from, to),
                    $"AllowedFrom({from}) and IsAllowed({from}, {to}) must agree");
            }
        }
    }
}
