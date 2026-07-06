using FluentAssertions;
using Tiketin.Web.Services;

namespace Tiketin.Tests.Unit;

public class KbSlugTests
{
    [Theory]
    [InlineData("Cara reset password", "cara-reset-password")]
    [InlineData("  Printer  Offline!  ", "printer-offline")]
    [InlineData("VPN: koneksi dari luar (FortiClient)", "vpn-koneksi-dari-luar-forticlient")]
    [InlineData("Akses WiFi & LAN", "akses-wifi-lan")]
    public void Slugify_produces_clean_kebab_case(string title, string expected)
    {
        KbService.Slugify(title).Should().Be(expected);
    }

    [Fact]
    public void Slugify_caps_length_to_leave_room_for_uniqueness_suffix()
    {
        var slug = KbService.Slugify(new string('a', 300));

        slug.Length.Should().BeLessThanOrEqualTo(160);
    }
}
