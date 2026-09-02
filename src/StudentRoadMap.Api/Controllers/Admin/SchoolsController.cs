using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Admin.Schools;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Admin.Schools.Create;
using StudentRoadMap.Application.Admin.Schools.Delete;
using StudentRoadMap.Application.Admin.Schools.GetById;
using StudentRoadMap.Application.Admin.Schools.List;
using StudentRoadMap.Application.Admin.Schools.RegenerateLink;
using StudentRoadMap.Application.Admin.Schools.ToggleActive;
using StudentRoadMap.Application.Admin.Schools.Update;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// Maktablar admin API'si — `docs/07-api-shartnoma.md` 3.1-bo'lim, `prompts/14`. Faqat
/// superadmin (`[Authorize(Policy = SuperAdminPolicy)]`, P13da tayyor). `docs/07` 4-bo'lim:
/// "Admin API (umumiy) 300/daqiqa" — `RateLimitSetup.AdminApi`.
/// </summary>
[ApiController]
[Route("api/admin/schools")]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class SchoolsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public SchoolsController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>`GET /api/admin/schools` — `docs/07` 3.1-bo'lim.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminSchoolListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<PagedResult<AdminSchoolListItemDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? region,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListSchoolsQuery(search, region, isActive, page, pageSize, sort);
        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`GET /api/admin/schools/{id}` — batafsil + statistika.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminSchoolDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminSchoolDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSchoolByIdQuery(id), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/schools` — `slug` avtomatik generatsiya (`prompts/14` MAXSUS DIQQAT #3).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminSchoolDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminSchoolDetailDto>> Create([FromBody] CreateSchoolRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>`PUT /api/admin/schools/{id}` — `slug`/havola tokeni o'zgarmaydi.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminSchoolDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminSchoolDetailDto>> Update(Guid id, [FromBody] UpdateSchoolRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/schools/{id}/regenerate-link` — yangi `accessToken` + `publicUrl` + QR kod.</summary>
    [HttpPost("{id:guid}/regenerate-link")]
    [ProducesResponseType(typeof(RegenerateSchoolLinkResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<RegenerateSchoolLinkResult>> RegenerateLink(Guid id, CancellationToken cancellationToken)
    {
        var command = new RegenerateSchoolLinkCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/schools/{id}/toggle-active` — faol/nofaol.</summary>
    [HttpPost("{id:guid}/toggle-active")]
    [ProducesResponseType(typeof(AdminSchoolDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminSchoolDetailDto>> ToggleActive(Guid id, CancellationToken cancellationToken)
    {
        var command = new ToggleSchoolActiveCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`DELETE /api/admin/schools/{id}` — soft delete; o'quvchisi bo'lsa `409 SCHOOL_HAS_STUDENTS`.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteSchoolCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}
