using System.ComponentModel.DataAnnotations;

namespace Tiketin.Web.Contracts;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record RefreshRequest(
    [Required] string RefreshToken);

public record AuthResponse(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken);
