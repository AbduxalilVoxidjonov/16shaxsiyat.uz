using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Assessments.Export;
using StudentRoadMap.Application.Admin.Students.Export;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// Eksport endpointlari — `docs/07-api-shartnoma.md` 3.2/3.3-bo'lim, `prompts/27`. Ataylab
/// ALOHIDA controller (`StudentsController`/`AssessmentsController` EMAS) — uch backend agenti
/// parallel ishlayotgani sabab (fayl egaligi chegarasi, topshiriq "MAXSUS DIQQAT" bo'limi):
/// yo'llar ASP.NET Core marshrutlashida controller nomi bilan bog'liq emas, faqat pastdagi
/// `[HttpGet]` shablon satriga bog'liq — shu sabab yo'llar `docs/07`ga aynan mos qoladi.
/// </summary>
[ApiController]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class ExportController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public ExportController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>
    /// `GET /api/admin/students/export?<filtrlar>` — `docs/07` 3.2-bo'lim: "`.xlsx` (filtr
    /// saqlanadi)". Filtr maydonlari `StudentsController.List` bilan AYNAN bir xil nomda —
    /// frontend joriy ro'yxat filtrini o'sha nomlar bilan qayta kodlab yuboradi
    /// (`useExportStudentsMutation.ts`, `buildStudentsQueryString`). `page`/`pageSize`/`sort`
    /// ataylab QABUL QILINMAYDI (bog'lanmagan query parametrlar ASP.NET Core'da xato bermaydi,
    /// jimgina e'tiborsiz qoldiriladi) — eksport sahifalanmaydi, FILTRGA mos BARCHA qatorni beradi.
    /// </summary>
    [HttpGet("api/admin/students/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> ExportStudents(
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
        CancellationToken cancellationToken)
    {
        var query = new ExportStudentsQuery(
            schoolId, grade, status, needsAttention, personalityType, activityLevel, gender, ageMin, ageMax,
            from, to, search, RequireAdminUserId(), ClientIp(), UserAgent());

        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        var file = result.Value;
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>`GET /api/admin/assessments/{id}/report.pdf` — `docs/07` 3.3-bo'lim: "PDF hisobot".</summary>
    [HttpGet("api/admin/assessments/{id:guid}/report.pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> AssessmentReportPdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GenerateAssessmentReportQuery(id), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        var file = result.Value;
        return File(file.Content, file.ContentType, file.FileName);
    }

    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}
