using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Dashboard;
using StudentRoadMap.Application.Admin.Dashboard.GetStats;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// Boshqaruv paneli statistikasi — `docs/07-api-shartnoma.md` 3.6-bo'lim, `prompts/15`. Faqat
/// superadmin. `docs/07` 4-bo'lim: "Admin API (umumiy) 300/daqiqa" — `RateLimitSetup.AdminApi`.
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// `GET /api/admin/dashboard/stats?from=&to=&source=` — `docs/07` 3.6-bo'lim to'liq javobi.
    /// `source` (2026-09-06) — KO'LAM: `school` (STANDART, maktab oqimi) yoki `public`
    /// (ommaviy makon). Ikki ko'lam raqamlari bitta javobda hech qachon aralashmaydi.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminDashboardStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<AdminDashboardStatsDto>> GetStats(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? source,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDashboardStatsQuery(from, to, source), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }
}
