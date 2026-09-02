using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Audit;
using StudentRoadMap.Application.Admin.Audit.List;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// Audit jurnali — `docs/07-api-shartnoma.md` 3.6-bo'lim, `docs/08-auth-va-xavfsizlik.md` §8,
/// `prompts/15`. Faqat superadmin. `docs/07` 4-bo'lim: "Admin API (umumiy) 300/daqiqa" —
/// `RateLimitSetup.AdminApi`.
/// </summary>
[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class AuditController : ControllerBase
{
    private readonly ISender _sender;

    public AuditController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>`GET /api/admin/audit-logs?action=&entityType=&from=&to=&page=` — `docs/07` 3.6-bo'lim.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminAuditLogItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<PagedResult<AdminAuditLogItemDto>>> List(
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListAuditLogsQuery(action, entityType, from, to, page, pageSize);
        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }
}
