using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.PublicUsers;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.PublicUsers.Logout;
using StudentRoadMap.Application.PublicUsers.Refresh;
using StudentRoadMap.Application.PublicUsers.TelegramLogin;

namespace StudentRoadMap.Api.Controllers;

/// <summary>
/// Ommaviy (tashqi) foydalanuvchi autentifikatsiyasi — Telegram Login Widget
/// (`docs/07` 1a-bo'lim, `docs/08` 2a-bo'lim).
///
/// `AuthController` (superadmin) dan MUTLAQO ajratilgan: boshqa marshrut prefiksi, boshqa
/// cookie (`PublicRefreshTokenCookie`), boshqa JWT `aud`/rol va boshqa rate limit siyosati.
/// Refresh token HECH QACHON javob tanasida qaytmaydi — faqat
/// `httpOnly; Secure; SameSite=Strict; Path=/api/auth/telegram` cookie'da.
/// </summary>
[ApiController]
[Route("api/auth/telegram")]
public sealed class PublicAuthController : ControllerBase
{
    private readonly ISender _sender;

    public PublicAuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// `POST /api/auth/telegram` — Telegram Login Widget ma'lumoti bilan kirish/ro'yxatdan
    /// o'tish. Autentifikatsiyasiz (bu KIRISH nuqtasi).
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitSetup.PublicTelegramAuth)]
    [ProducesResponseType(typeof(TelegramLoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<TelegramLoginResult>> Login([FromBody] TelegramLoginRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        Response.Cookies.Append(
            PublicRefreshTokenCookie.Name,
            result.Value.RefreshToken,
            PublicRefreshTokenCookie.BuildOptions(result.Value.RefreshTokenExpiresAt));

        return Ok(result.Value);
    }

    /// <summary>
    /// `POST /api/auth/telegram/refresh` — tana bo'sh; refresh token `httpOnly` cookie'dan
    /// o'qiladi → yangi `accessToken` + rotatsiya qilingan cookie.
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitSetup.PublicUserApi)]
    [ProducesResponseType(typeof(PublicRefreshResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<PublicRefreshResult>> Refresh(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(PublicRefreshTokenCookie.Name, out var rawRefreshToken);

        var command = new PublicRefreshCommand(rawRefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            // Yaroqsiz/qayta ishlatilgan token — eski cookie'ni ham tozalab qo'yamiz.
            Response.Cookies.Delete(PublicRefreshTokenCookie.Name, PublicRefreshTokenCookie.BuildDeleteOptions());
            return this.ToProblem(result.Error);
        }

        Response.Cookies.Append(
            PublicRefreshTokenCookie.Name,
            result.Value.RefreshToken,
            PublicRefreshTokenCookie.BuildOptions(result.Value.RefreshTokenExpiresAt));

        return Ok(result.Value);
    }

    /// <summary>`POST /api/auth/telegram/logout` — idempotent; cookie har doim tozalanadi.</summary>
    [HttpPost("logout")]
    [EnableRateLimiting(RateLimitSetup.PublicUserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(PublicRefreshTokenCookie.Name, out var rawRefreshToken);

        await _sender.Send(new PublicLogoutCommand(rawRefreshToken), cancellationToken).ConfigureAwait(false);

        Response.Cookies.Delete(PublicRefreshTokenCookie.Name, PublicRefreshTokenCookie.BuildDeleteOptions());

        return NoContent();
    }
}
