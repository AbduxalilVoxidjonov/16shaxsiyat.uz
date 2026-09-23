using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Admin.PublicSpace;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.PublicSpace;
using StudentRoadMap.Application.Admin.PublicSpace.AssignProgram;
using StudentRoadMap.Application.Admin.PublicSpace.Get;
using StudentRoadMap.Application.Admin.PublicSpace.ListUsers;
using StudentRoadMap.Application.Admin.PublicSpace.SetShowResult;
using StudentRoadMap.Application.Admin.PublicSpace.SetTest;
using StudentRoadMap.Application.Admin.PublicSpace.UnassignProgram;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// Ommaviy makon admin API'si (2026-09-06) — `SchoolsController` dan ATAYLAB AJRATILGAN.
/// Ommaviy makon `schools` jadvalidagi qator bo'lsa ham (`SchoolKind` izohi: nima uchun
/// `SchoolId` nullable qilinmadi), u MAKTAB EMAS: unda viloyat/tuman, maktab raqami, aloqa
/// shaxsi, kirish kodi va maxfiy havola tushunchalari yo'q, o'chirish/faolsizlantirish esa
/// domen darajasida taqiqlangan (`SCHOOL_PUBLIC_SPACE_PROTECTED`).
///
/// <para>
/// **Bu yerda MAVJUD EMAS va ataylab qo'shilmagan:** `DELETE` (makonni o'chirish) va
/// `toggle-active`. Domen ularni `DomainException` bilan rad etardi — endpointning o'zi
/// bo'lmasligi esa adminni "nega ishlamadi?" degan savoldan butunlay xalos qiladi.
/// </para>
/// </summary>
[ApiController]
[Route("api/admin/public-space")]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class PublicSpaceController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public PublicSpaceController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>`GET /api/admin/public-space` — holat, dasturlar, sozlamalar, statistika.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AdminPublicSpaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminPublicSpaceDto>> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPublicSpaceQuery(), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `GET /api/admin/public-space/users` — ro'yxatdan o'tgan foydalanuvchilar: kim, qachon,
    /// nechta sessiya, oxirgi sessiya va (yakunlanmagan bo'lsa) qayerda to'xtagan
    /// (`docs/07` 3.7-bo'lim). `status`: `all|never_started|in_progress|completed|deleted`
    /// (`deleted` — 2026-09-08, faqat "ma'lumotimni o'chiring" qilganlar);
    /// `sort`: `registeredAt` (standart `-registeredAt`) yoki `lastLoginAt`.
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<AdminPublicUserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<PagedResult<AdminPublicUserListItemDto>>> ListUsers(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListPublicSpaceUsersQuery(search, status, page, pageSize, sort);
        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `POST /api/admin/public-space/programs/{programId}` — dasturni biriktiradi (idempotent).
    /// Mexanizm mavjud `school_programs` biriktirmasi (`AssignProgramSchoolCommand`).
    /// </summary>
    [HttpPost("programs/{programId:guid}")]
    [ProducesResponseType(typeof(AdminPublicSpaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminPublicSpaceDto>> AssignProgram(Guid programId, CancellationToken cancellationToken)
    {
        var command = new AssignPublicSpaceProgramCommand(programId, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`DELETE /api/admin/public-space/programs/{programId}` — biriktirishni olib tashlaydi (idempotent).</summary>
    [HttpDelete("programs/{programId:guid}")]
    [ProducesResponseType(typeof(AdminPublicSpaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminPublicSpaceDto>> UnassignProgram(Guid programId, CancellationToken cancellationToken)
    {
        var command = new UnassignPublicSpaceProgramCommand(programId, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `POST /api/admin/public-space/tests/{testId}` — testni ommaviy makonga biriktiradi
    /// (idempotent; 2026-09-23, "Dasturlar" bo'limi o'rniga). Test dasturi bo'lmasa yaratiladi.
    /// </summary>
    [HttpPost("tests/{testId:guid}")]
    [ProducesResponseType(typeof(AdminPublicSpaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminPublicSpaceDto>> AssignTest(Guid testId, CancellationToken cancellationToken)
    {
        var command = new SetPublicSpaceTestCommand(testId, Linked: true, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`DELETE /api/admin/public-space/tests/{testId}` — testni ommaviy makondan olib tashlaydi (idempotent).</summary>
    [HttpDelete("tests/{testId:guid}")]
    [ProducesResponseType(typeof(AdminPublicSpaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminPublicSpaceDto>> UnassignTest(Guid testId, CancellationToken cancellationToken)
    {
        var command = new SetPublicSpaceTestCommand(testId, Linked: false, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `PUT /api/admin/public-space/show-result` — foydalanuvchi o'z natijasini ko'radimi.
    /// Ilgari faqat bazadan qo'lda o'zgartirilardi.
    /// </summary>
    [HttpPut("show-result")]
    [ProducesResponseType(typeof(AdminPublicSpaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AdminPublicSpaceDto>> SetShowResult(
        [FromBody] SetPublicSpaceShowResultRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetPublicSpaceShowResultCommand(request.Enabled, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}
