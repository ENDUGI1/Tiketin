using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tiketin.Web.Contracts;
using Tiketin.Web.Data;
using Tiketin.Web.Domain;
using Tiketin.Web.Infrastructure;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Services;

public class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    IJwtTokenService tokenService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, password))
        {
            throw new ForbiddenException("Email atau kata sandi salah.");
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = tokenService.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive || !stored.User.IsActive)
        {
            throw new ForbiddenException("Refresh token tidak valid.");
        }

        stored.RevokedAt = DateTimeOffset.UtcNow;
        return await IssueTokensAsync(stored.User, ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(AppUser user, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresIn) = tokenService.CreateAccessToken(user, roles);

        var rawRefresh = tokenService.CreateRefreshTokenValue();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(rawRefresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });
        await db.SaveChangesAsync(ct);

        return new AuthResponse(accessToken, expiresIn, rawRefresh);
    }
}
