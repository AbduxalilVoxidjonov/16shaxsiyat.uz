using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.PublicUsers;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.GetStudentResult;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Application.PublicUsers.DeleteAccount;
using StudentRoadMap.Application.PublicUsers.GetAssessmentResult;
using StudentRoadMap.Application.PublicUsers.GetProfile;
using StudentRoadMap.Application.PublicUsers.ListAssessments;
using StudentRoadMap.Application.PublicUsers.TelegramLogin;

namespace StudentRoadMap.Api.Controllers;

/// <summary>
/// Ommaviy foydalanuvchining SHAXSIY KABINETI — `docs/07` 5-bo'lim. Barcha amallar
/// `PublicUserPolicy` bilan himoyalangan: faqat `PublicBearer` sxemasi + `PublicUser` roli
/// (superadmin tokeni bu yerga `aud` mos kelmagani uchun UMUMAN o'tmaydi).
///
/// **Egalik hech qachon so'rov tanasidan/URL'dan olinmaydi** — har doim JWT `sub` claim'idan
/// (`ICurrentUser.PublicUserId`). `assessments/{id}/result` dagi `{id}` — tanlash kaliti,
/// egalik isboti EMAS: begona identifikator uchun handler `404` qaytaradi
/// (`GetMyAssessmentResultQuery` izohi).
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize(Policy = JwtAuthenticationSetup.PublicUserPolicy)]
[EnableRateLimiting(RateLimitSetup.PublicUserApi)]
public sealed class MeController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public MeController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>`GET /api/me` — profil (Telegram ma'lumotlari, ro'yxatdan o'tgan sana).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PublicUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<PublicUserDto>> Me(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyProfileQuery(RequirePublicUserId()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`GET /api/me/assessments` — foydalanuvchining test sessiyalari tarixi.</summary>
    [HttpGet("assessments")]
    [ProducesResponseType(typeof(ListMyAssessmentsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<ListMyAssessmentsResult>> Assessments(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListMyAssessmentsQuery(RequirePublicUserId()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `GET /api/me/assessments/{id}/result` — bitta sessiyaning qisqartirilgan natijasi.
    /// Tahlil tayyor bo'lmasa tana yo'q `202 Accepted` (maktab oqimidagi bilan bir xil
    /// semantika); begona/mavjud bo'lmagan `id` uchun `404` (`403` EMAS).
    /// </summary>
    [HttpGet("assessments/{id:guid}/result")]
    [ProducesResponseType(typeof(GetStudentResultResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<GetStudentResultResult>> AssessmentResult(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyAssessmentResultQuery(RequirePublicUserId(), id), cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return result.Value is null ? Accepted() : Ok(result.Value);
    }

    /// <summary>
    /// `POST /api/me/sessions` — ommaviy (maktabsiz) test sessiyasini ochish. Javob shakli
    /// maktab oqimidagi bilan AYNAN bir xil (`StartSessionResult`) — frontend keyingi
    /// qadamlarda o'sha `X-Session-Token` oqimidan foydalanadi (`/api/public/sessions/*`).
    /// </summary>
    [HttpPost("sessions")]
    [EnableRateLimiting(RateLimitSetup.PublicStartSession)]
    [ProducesResponseType(typeof(StartSessionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(StartSessionResult), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<StartSessionResult>> StartSession([FromBody] StartPublicSessionRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(
            RequirePublicUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        // Maktab oqimidagi bilan bir xil qoida: davom ettirilgan sessiya yangi resurs
        // yaratmaydi → `200`, yangi sessiya → `201`.
        return result.Value.Resumed
            ? Ok(result.Value)
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// `DELETE /api/me` — foydalanuvchi o'z ma'lumotini o'chiradi (anonimlashtirish).
    /// Idempotent → har doim `204`.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        var command = new DeleteMyAccountCommand(RequirePublicUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());

        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `[Authorize(Policy = PublicUserPolicy)]` bilan himoyalangan amallarda `sub` claim va
    /// `role = PublicUser` HAR DOIM mavjud — topilmasa autentifikatsiya qatlamida xato
    /// bo'lgan bo'lardi (`AuthController.RequireAdminUserId` bilan bir xil naqsh).
    /// </summary>
    private Guid RequirePublicUserId() =>
        _currentUser.PublicUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");
}
