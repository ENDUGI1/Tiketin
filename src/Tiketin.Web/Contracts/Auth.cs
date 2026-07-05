using System.ComponentModel.DataAnnotations;

namespace Tiketin.Web.Contracts;

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public record RefreshRequest(
    [property: Required] string RefreshToken);

public record AuthResponse(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken);
