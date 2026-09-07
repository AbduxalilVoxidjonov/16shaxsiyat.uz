using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Admin.Students.Delete;
using StudentRoadMap.Application.Admin.Students.GetById;
using StudentRoadMap.Application.Admin.Students.List;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// O'quvchilar admin API'si — `docs/07-api-shartnoma.md` 3.2-bo'lim, `prompts/14`. Faqat superadmin.
/// `docs/07` 4-bo'lim: "Admin API (umumiy) 300/daqiqa" — `RateLimitSetup.AdminApi`.
/// </summary>
[ApiController]
[Route("api/admin/students")]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class StudentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public StudentsController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>
    /// `GET /api/admin/students` — `docs/07` 3.2-bo'lim. FAQAT maktab o'quvchilari (ommaviy makon
    /// foydalanuvchilari — `GET /api/admin/public-space/users`). Filtrlar: `status`
    /// (`AssessmentStatus` nomi), `activityLevel` (`Passive|LowActive|Moderate|Active|HighlyActive`),
    /// `gender` (`Male|Female`), `ageMin`/`ageMax` (6–99, `ageMin &lt;= ageMax`; noto'g'ri → 400).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminStudentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<PagedResult<AdminStudentListItemDto>>> List(
        [FromQuery] Guid? schoolId,
        [FromQuery] int? grade,
        [FromQuery] string? status,
        [FromQuery] bool? needsAttention,
        [FromQuery] string? personalityType,
        [FromQuery] string? activityLevel,
        [FromQuery] string? gender,
        [FromQuery] int? ageMin,
        [FromQuery] int? ageMax,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListStudentsQuery(
            schoolId, grade, status, needsAttention, personalityType, activityLevel, gender, ageMin, ageMax,
            from, to, search, page, pageSize, sort);
        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`GET /api/admin/students/{id}` — individual profil (`docs/07` 3.2-bo'lim).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminStudentProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminStudentProfileDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStudentByIdQuery(id), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `DELETE /api/admin/students/{id}?hard=true` — `docs/07` 3.2-bo'lim. `hard=true` — o'quvchi
    /// so'rovi bo'yicha to'liq o'chirish (`prompts/14` MAXSUS DIQQAT #5).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] bool hard = false, CancellationToken cancellationToken = default)
    {
        var command = new DeleteStudentCommand(id, hard, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}
