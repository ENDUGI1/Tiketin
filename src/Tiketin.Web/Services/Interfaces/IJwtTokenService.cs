using Tiketin.Web.Domain;

namespace Tiketin.Web.Services.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Creates a signed access token containing sub, email, name and role claims.</summary>
    (string Token, int ExpiresInSeconds) CreateAccessToken(AppUser user, IEnumerable<string> roles);

    /// <summary>Generates a cryptographically random refresh token value (raw, unhashed).</summary>
    string CreateRefreshTokenValue();

    /// <summary>SHA-256 hash used to store and look up refresh tokens.</summary>
    string HashRefreshToken(string rawToken);
}
