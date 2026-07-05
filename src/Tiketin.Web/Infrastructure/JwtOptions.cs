namespace Tiketin.Web.Infrastructure;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "tiketin";
    public string Audience { get; set; } = "tiketin";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
