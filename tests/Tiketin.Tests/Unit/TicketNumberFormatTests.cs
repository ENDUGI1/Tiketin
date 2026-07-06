using FluentAssertions;
using Tiketin.Web.Infrastructure;

namespace Tiketin.Tests.Unit;

public class TicketNumberFormatTests
{
    [Theory]
    [InlineData("202607", 1, "TKT-202607-0001")]
    [InlineData("202607", 42, "TKT-202607-0042")]
    [InlineData("202612", 999, "TKT-202612-0999")]
    [InlineData("202701", 1000, "TKT-202701-1000")]
    public void Format_pads_the_sequence_to_four_digits(string yearMonth, long value, string expected)
    {
        TicketNumberGenerator.Format(yearMonth, value).Should().Be(expected);
    }

    [Fact]
    public void Format_does_not_truncate_when_a_month_exceeds_9999_tickets()
    {
        TicketNumberGenerator.Format("202607", 12345).Should().Be("TKT-202607-12345");
    }
}
