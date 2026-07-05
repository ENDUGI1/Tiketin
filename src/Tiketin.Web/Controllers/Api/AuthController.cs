using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tiketin.Web.Contracts;
using Tiketin.Web.Services.Interfaces;

namespace Tiketin.Web.Controllers.Api;

/// <summary>Authentication endpoints for API (JWT) clients.</summary>
[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Exchanges email + password for a JWT access token and a refresh token.</summary>
    /// <response code="200">Credentials valid; token pair returned.</response>
    /// <response code="400">Request body invalid.</response>
    /// <response code="403">Credentials invalid or account inactive.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, ct);
        return Ok(new ApiResponse<AuthResponse>(result));
    }

    /// <summary>Rotates a refresh token for a new access + refresh token pair.</summary>
    /// <response code="200">Token rotated.</response>
    /// <response code="400">Request body invalid.</response>
    /// <response code="403">Refresh token unknown, expired or revoked.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshAsync(request.RefreshToken, ct);
        return Ok(new ApiResponse<AuthResponse>(result));
    }
}
