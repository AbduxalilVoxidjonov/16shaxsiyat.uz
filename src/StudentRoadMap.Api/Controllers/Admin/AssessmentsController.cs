using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Admin.Ai;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Ai;
using StudentRoadMap.Application.Admin.Assessments;
using StudentRoadMap.Application.Admin.Assessments.Delete;
using StudentRoadMap.Application.Admin.Assessments.GetAnswers;
using StudentRoadMap.Application.Admin.Assessments.GetById;
using StudentRoadMap.Application.Admin.Assessments.List;
using StudentRoadMap.Application.Admin.Assessments.RecalculateScores;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// Sessiyalar admin API'si — `docs/07-api-shartnoma.md` 3.3-bo'lim, `prompts/15`. Faqat
/// superadmin. `docs/07` 4-bo'lim: "Admin API (umumiy) 300/daqiqa" — `RateLimitSetup.AdminApi`.
/// `prompts/15` qamrovi: `List`/`GetById`/`GetAnswers`/`RecalculateScores`/`Delete` —
/// `rerun-analysis` (P18, shu yerga qo'shildi — `ai-integration` agenti hududi:
/// `Application/Admin/Ai/RerunAnalysis/**`) va `report.pdf` (P27-29) shu promptga kirmaydi.
/// </summary>
[ApiController]
[Route("api/admin/assessments")]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class AssessmentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public AssessmentsController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>`GET /api/admin/assessments` — `docs/07` 3.3-bo'lim.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminAssessmentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<PagedResult<AdminAssessmentListItemDto>>> List(
        [FromQuery] Guid? schoolId,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListAssessmentsQuery(schoolId, status, from, to, page, pageSize, sort);
        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `GET /api/admin/assessments/{id}` — `docs/07` 3.3-bo'lim: to'liq detal. `latestAssessment`
    /// yadrosi (`{id, results, aiAnalysis, aiHistory}`) + sessiya sarlavhasi va `tests[]`
    /// (`AdminAssessmentDetailDto`, 2026-09-03).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminAssessmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminAssessmentDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAssessmentByIdQuery(id), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `GET /api/admin/assessments/{id}/answers?testCode=` — savolma-savol javoblar va ularning
    /// tahlili (audit uchun). Javob konvert (`AdminAssessmentAnswersDto`): qatorlar + sessiya
    /// signallari + shkala signallari + `ScoringConstants` chegaralari.
    /// </summary>
    [HttpGet("{id:guid}/answers")]
    [ProducesResponseType(typeof(AdminAssessmentAnswersDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminAssessmentAnswersDto>> GetAnswers(
        Guid id,
        [FromQuery] string? testCode,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAssessmentAnswersQuery(id, testCode), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `POST /api/admin/assessments/{id}/recalculate-scores` — scoring versiyasi o'zgarganda
    /// qayta hisoblash (`prompts/15`).
    /// </summary>
    [HttpPost("{id:guid}/recalculate-scores")]
    [ProducesResponseType(typeof(AdminRecalculateScoresResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminRecalculateScoresResultDto>> RecalculateScores(Guid id, CancellationToken cancellationToken)
    {
        var command = new RecalculateAssessmentScoresCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `POST /api/admin/assessments/{id}/rerun-analysis` — `docs/07` §3.3: `{ provider?,
    /// promptVersion? }` → `202`. AI qayta tahlili darhol emas — fon navbatiga qo'yiladi
    /// (P18, `Infrastructure/Jobs`).
    /// </summary>
    [HttpPost("{id:guid}/rerun-analysis")]
    [ProducesResponseType(typeof(RerunAnalysisResultDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<RerunAnalysisResultDto>> RerunAnalysis(
        Guid id, [FromBody] RerunAnalysisRequest? request, CancellationToken cancellationToken)
    {
        var command = (request ?? new RerunAnalysisRequest(null, null)).ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return Accepted(result.Value);
    }

    /// <summary>`DELETE /api/admin/assessments/{id}` — soft delete.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteAssessmentCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}
