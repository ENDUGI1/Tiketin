using Tiketin.Web.Contracts;

namespace Tiketin.Web.Services.Interfaces;

public interface IAuthService
{
    /// <summary>Validates credentials and issues an access + refresh token pair.</summary>
    /// <exception cref="Domain.ForbiddenException">Credentials invalid or account inactive.</exception>
    Task<AuthResponse> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Rotates a refresh token: revokes the presented one and issues a new pair.</summary>
    /// <exception cref="Domain.ForbiddenException">Token unknown, expired or revoked.</exception>
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
}
