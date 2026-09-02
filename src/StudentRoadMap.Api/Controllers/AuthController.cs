using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Auth;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.ChangePassword;
using StudentRoadMap.Application.Identity.DisableTotp;
using StudentRoadMap.Application.Identity.EnableTotp;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Application.Identity.Logout;
using StudentRoadMap.Application.Identity.Me;
using StudentRoadMap.Application.Identity.Refresh;

namespace StudentRoadMap.Api.Controllers;

/// <summary>
/// Superadmin autentifikatsiyasi — `docs/07-api-shartnoma.md` 2-bo'lim,
/// `docs/08-auth-va-xavfsizlik.md` 2-bo'lim, `prompts/13-auth-va-jwt.md`. Refresh token
/// HECH QACHON javob tanasida qaytmaydi — faqat `httpOnly; Secure; SameSite=Strict` cookie
/// (`RefreshTokenCookie`, `Path=/api/auth`).
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public AuthController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>`POST /api/auth/login` — `docs/07` 2-bo'lim.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitSetup.AdminLogin)]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<LoginResult>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        Response.Cookies.Append(RefreshTokenCookie.Name, result.Value.RefreshToken, RefreshTokenCookie.BuildOptions(result.Value.RefreshTokenExpiresAt));

        return Ok(result.Value);
    }

    /// <summary>
    /// `POST /api/auth/refresh` — `docs/07` 2-bo'lim. Tana bo'sh; refresh token `httpOnly`
    /// cookie'dan o'qiladi → yangi `accessToken` + rotatsiya qilingan cookie.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<RefreshResult>> Refresh(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshTokenCookie.Name, out var rawRefreshToken);

        var command = new RefreshCommand(rawRefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            // Yaroqsiz/qayta ishlatilgan token — eski cookie'ni ham tozalab qo'yamiz.
            Response.Cookies.Delete(RefreshTokenCookie.Name, RefreshTokenCookie.BuildDeleteOptions());
            return this.ToProblem(result.Error);
        }

        Response.Cookies.Append(RefreshTokenCookie.Name, result.Value.RefreshToken, RefreshTokenCookie.BuildOptions(result.Value.RefreshTokenExpiresAt));

        return Ok(result.Value);
    }

    /// <summary>`POST /api/auth/logout` — `docs/07` 2-bo'lim. Idempotent; cookie har doim tozalanadi.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshTokenCookie.Name, out var rawRefreshToken);

        await _sender.Send(new LogoutCommand(rawRefreshToken), cancellationToken).ConfigureAwait(false);

        Response.Cookies.Delete(RefreshTokenCookie.Name, RefreshTokenCookie.BuildDeleteOptions());

        return NoContent();
    }

    /// <summary>`GET /api/auth/me` — `docs/07` 2-bo'lim.</summary>
    [HttpGet("me")]
    [Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
    [EnableRateLimiting(RateLimitSetup.AdminApi)]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<AdminUserDto>> Me(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MeQuery(RequireAdminUserId()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/auth/change-password` — `docs/07` 2-bo'lim.</summary>
    [HttpPost("change-password")]
    [Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
    [EnableRateLimiting(RateLimitSetup.AdminApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(request.ToCommand(RequireAdminUserId()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/auth/totp/enable` — `docs/07` 2-bo'lim.</summary>
    [HttpPost("totp/enable")]
    [Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
    [EnableRateLimiting(RateLimitSetup.AdminApi)]
    [ProducesResponseType(typeof(EnableTotpResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<EnableTotpResult>> EnableTotp(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new EnableTotpCommand(RequireAdminUserId()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/auth/totp/disable` — `docs/07` 2-bo'lim.</summary>
    [HttpPost("totp/disable")]
    [Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
    [EnableRateLimiting(RateLimitSetup.AdminApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DisableTotp([FromBody] DisableTotpRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(request.ToCommand(RequireAdminUserId()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `[Authorize(Policy = SuperAdminPolicy)]` bilan himoyalangan endpointlarda JWT `sub`
    /// claim'i har doim mavjud — topilmasa autentifikatsiya qatlamida xato bo'lgan bo'lardi.
    /// </summary>
    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");
}
