using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Public;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Public.GetSchoolInfo;
using StudentRoadMap.Application.Public.GetSession;
using StudentRoadMap.Application.Public.StartSession;

namespace StudentRoadMap.Api.Controllers;

/// <summary>
/// O'quvchi (ommaviy) oqimi — maktab havolasini tekshirish, sessiya ochish, sessiya holatini
/// o'qish (`docs/07-api-shartnoma.md` 1.1–1.3-bo'lim, `prompts/10`). Boshqa endpointlar
/// (savollar, javob saqlash, testni yakunlash) keyingi promptlarda qo'shiladi.
/// </summary>
[ApiController]
[Route("api/public")]
public sealed class PublicSessionController : ControllerBase
{
    private readonly ISender _sender;

    public PublicSessionController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>`GET /api/public/schools/{slug}?k={accessToken}` — `docs/07` 1.1-bo'lim.</summary>
    [HttpGet("schools/{slug}")]
    [EnableRateLimiting(RateLimitSetup.PublicSchoolInfo)]
    public async Task<IActionResult> GetSchoolInfo(string slug, [FromQuery(Name = "k")] string? k, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSchoolInfoQuery(slug, k ?? string.Empty), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `POST /api/public/sessions` — `docs/07` 1.2-bo'lim. Anketa + sessiya ochish.
    /// So'rov tanasi `StartSessionRequest` (`IpAddress`/`UserAgent`siz, `prompts/10` tuzatish
    /// #3) — bu ikkalasini kontroller `HttpContext`dan o'zi qo'shib `StartSessionCommand`ga
    /// aylantiradi, mijoz ularni Swagger'da ko'rmaydi va yubora olmaydi.
    /// </summary>
    [HttpPost("sessions")]
    [EnableRateLimiting(RateLimitSetup.PublicStartSession)]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        // Yangi sessiya — 201 Created; mavjud tugallanmagan sessiya davom ettirilsa — 200 OK
        // (`docs/07` 1.2 faqat "201"ni ko'rsatadi, lekin `resumed: true` holati yangi resurs
        // yaratmaydi — REST semantikasiga ko'ra 200 tanlandi; PM'ga savol).
        return result.Value.Resumed
            ? Ok(result.Value)
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>`GET /api/public/sessions/me` — `docs/07` 1.3-bo'lim. `X-Session-Token` bo'yicha holatni tiklaydi.</summary>
    [HttpGet("sessions/me")]
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    public async Task<IActionResult> GetSessionState(CancellationToken cancellationToken)
    {
        // `assessmentId` URL/tanadan emas — `SessionTokenAuthenticationHandler` autentifikatsiya
        // paytida `HttpContext.Items`ga qo'ygan (IDOR himoyasi, `CLAUDE.md` 8-qoida).
        var assessmentId = (Guid)HttpContext.Items["AssessmentId"]!;

        var result = await _sender.Send(new GetSessionStateQuery(assessmentId), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }
}
