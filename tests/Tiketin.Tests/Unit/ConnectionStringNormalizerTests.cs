using FluentAssertions;
using Tiketin.Web.Infrastructure;

namespace Tiketin.Tests.Unit;

public class ConnectionStringNormalizerTests
{
    [Fact]
    public void Keyword_format_passes_through_untouched()
    {
        const string keyword = "Host=localhost;Port=5432;Database=tiketin;Username=tiketin;Password=x";

        ConnectionStringNormalizer.Normalize(keyword).Should().Be(keyword);
    }

    [Fact]
    public void Neon_style_uri_converts_to_keyword_format()
    {
        var result = ConnectionStringNormalizer.Normalize(
            "postgresql://neondb_owner:npg_Secret123@ep-cool-name-a1b2c3.ap-southeast-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require");

        result.Should().Be(
            "Host=ep-cool-name-a1b2c3.ap-southeast-1.aws.neon.tech;Port=5432;Database=neondb;" +
            "Username=neondb_owner;Password=npg_Secret123;SSL Mode=require;Channel Binding=require");
    }

    [Fact]
    public void Explicit_port_and_encoded_password_survive_conversion()
    {
        var result = ConnectionStringNormalizer.Normalize(
            "postgres://user:p%40ss%2Fword@db.example.com:6543/mydb");

        result.Should().Contain("Port=6543")
            .And.Contain("Password=p@ss/word")
            .And.Contain("Database=mydb");
    }

    [Fact]
    public void Null_and_empty_are_returned_as_is()
    {
        ConnectionStringNormalizer.Normalize(null).Should().BeNull();
        ConnectionStringNormalizer.Normalize("").Should().Be("");
    }
}
