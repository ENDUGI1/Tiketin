namespace Tiketin.Web.Domain;

/// <summary>Opaque rotating refresh token for the JWT API auth flow.</summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    /// <summary>SHA-256 hash of the token value; the raw value is never stored.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
