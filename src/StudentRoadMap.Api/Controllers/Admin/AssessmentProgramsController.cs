using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Admin.Programs;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Application.Admin.Programs.AddTest;
using StudentRoadMap.Application.Admin.Programs.Archive;
using StudentRoadMap.Application.Admin.Programs.AssignSchool;
using StudentRoadMap.Application.Admin.Programs.Create;
using StudentRoadMap.Application.Admin.Programs.GetById;
using StudentRoadMap.Application.Admin.Programs.Impact;
using StudentRoadMap.Application.Admin.Programs.List;
using StudentRoadMap.Application.Admin.Programs.Publish;
using StudentRoadMap.Application.Admin.Programs.RemoveTest;
using StudentRoadMap.Application.Admin.Programs.ReorderTests;
using StudentRoadMap.Application.Admin.Programs.ToggleActive;
using StudentRoadMap.Application.Admin.Programs.UnassignSchool;
using StudentRoadMap.Application.Admin.Programs.Update;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// Dastur (`AssessmentProgram`) admin API'si — minimal (to'liq UI P35da), `prompts/34` E15-band.
/// Faqat superadmin. `docs/07` 4-bo'lim: "Admin API (umumiy) 300/daqiqa" — `RateLimitSetup.AdminApi`.
/// </summary>
[ApiController]
[Route("api/admin/programs")]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class AssessmentProgramsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public AssessmentProgramsController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>
    /// `GET /api/admin/programs?search=&amp;state=&amp;page=&amp;pageSize=&amp;sort=`.
    /// `state` — YAGONA holat filtri (`Draft` · `Active` · `Paused` · `Archived`).
    /// Eski `status`/`isActive` parametrlari 2026-09-06 da OLIB TASHLANDI (`ListProgramsQuery` izohi).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminProgramListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<PagedResult<AdminProgramListItemDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? state,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListProgramsQuery(search, state, page, pageSize, sort), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`GET /api/admin/programs/{id}`.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetProgramByIdQuery(id), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `GET /api/admin/programs/{id}/impact?action=deactivate|archive|makeAssigned` —
    /// amal NECHTA maktabni havolasiz qoldirishini OLDINDAN aytadi (`docs/07` 3.5, 2026-09-03).
    /// Read-only: amalni taqiqlamaydi, faqat oqibatni ko'rsatadi.
    /// </summary>
    [HttpGet("{id:guid}/impact")]
    [ProducesResponseType(typeof(AdminProgramImpactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminProgramImpactDto>> Impact(Guid id, [FromQuery] string action, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetProgramImpactQuery(id, action ?? string.Empty), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/programs` — har doim `Kind = Custom`/`Status = Draft`.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> Create([FromBody] CreateProgramRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>`PUT /api/admin/programs/{id}` — `Code`/`Kind`/`IsSystem` o'zgarmaydi.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> Update(Guid id, [FromBody] UpdateProgramRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/programs/{id}/publish` — kamida bitta test bo'lmasa `400 PROGRAM_NOT_PUBLISHABLE`.</summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PublishProgramCommand(id, RequireAdminUserId(), ClientIp(), UserAgent()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/programs/{id}/archive`.</summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ArchiveProgramCommand(id, RequireAdminUserId(), ClientIp(), UserAgent()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `POST /api/admin/programs/{id}/toggle-active` — `Active ⇄ Paused`. Faqat nashr
    /// qilingan (`Published`) dasturda: `Draft`/`Archived` da `409 PROGRAM_INVALID_TRANSITION`.
    /// </summary>
    [HttpPost("{id:guid}/toggle-active")]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> ToggleActive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ToggleProgramActiveCommand(id, RequireAdminUserId(), ClientIp(), UserAgent()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/programs/{id}/tests` — tizim dasturida `409 SYSTEM_PROGRAM_LOCKED`.</summary>
    [HttpPost("{id:guid}/tests")]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> AddTest(Guid id, [FromBody] AddProgramTestRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`DELETE /api/admin/programs/{id}/tests/{testDefinitionId}` — tizim dasturida `409 SYSTEM_PROGRAM_LOCKED`.</summary>
    [HttpDelete("{id:guid}/tests/{testDefinitionId:guid}")]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> RemoveTest(Guid id, Guid testDefinitionId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RemoveProgramTestCommand(id, testDefinitionId, RequireAdminUserId(), ClientIp(), UserAgent()),
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/programs/{id}/tests/reorder` — tizim dasturida `409 SYSTEM_PROGRAM_LOCKED`.</summary>
    [HttpPost("{id:guid}/tests/reorder")]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> ReorderTests(Guid id, [FromBody] ReorderProgramTestsRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/programs/{id}/schools/{schoolId}` — idempotent biriktirish.</summary>
    [HttpPost("{id:guid}/schools/{schoolId:guid}")]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> AssignSchool(Guid id, Guid schoolId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AssignProgramSchoolCommand(id, schoolId, RequireAdminUserId(), ClientIp(), UserAgent()),
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`DELETE /api/admin/programs/{id}/schools/{schoolId}` — idempotent olib tashlash.</summary>
    [HttpDelete("{id:guid}/schools/{schoolId:guid}")]
    [ProducesResponseType(typeof(AdminProgramDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminProgramDetailDto>> UnassignSchool(Guid id, Guid schoolId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UnassignProgramSchoolCommand(id, schoolId, RequireAdminUserId(), ClientIp(), UserAgent()),
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}
