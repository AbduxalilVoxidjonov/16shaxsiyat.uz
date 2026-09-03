using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Public;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Public.CompleteSession;
using StudentRoadMap.Application.Public.CompleteTest;
using StudentRoadMap.Application.Public.GetSchoolInfo;
using StudentRoadMap.Application.Public.GetSession;
using StudentRoadMap.Application.Public.GetStudentResult;
using StudentRoadMap.Application.Public.GetTestQuestions;
using StudentRoadMap.Application.Public.SaveAnswers;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Application.Public.StartTest;

namespace StudentRoadMap.Api.Controllers;

/// <summary>
/// O'quvchi (ommaviy) oqimi — maktab havolasini tekshirish, sessiya ochish, sessiya holatini
/// o'qish (`docs/07-api-shartnoma.md` 1.1–1.3-bo'lim, `prompts/10`), test boshlash, savollarni
/// olish va javoblarni saqlash (`docs/07` 1.4–1.6-bo'lim, `prompts/11`). Testni/sessiyani
/// yakunlash va qisqartirilgan natija (1.7/1.8/1.9-bo'lim, `prompts/12`).
///
/// **Swagger javob sxemalari:** har endpoint `ActionResult&lt;T&gt;` qaytaradi va
/// `[ProducesResponseType]` bilan HAQIQIY (handler kodidan tekshirilgan) status kodlari
/// e'lon qilinadi — muvaffaqiyat DTO tipi bilan, xatolar `ProblemDetails` (`application/problem+json`)
/// bilan. Bu shunchaki hujjat emas: `npm run generate:api` shu sxemalardan TS tiplarini chiqaradi
/// (`PM.md` §9 sifat darvozasi #4) — sxema bo'lmasa generatsiya qilingan tiplar bo'sh qoladi.
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
    [ProducesResponseType(typeof(GetSchoolInfoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    // `409 NO_PROGRAM_AVAILABLE` — havola to'g'ri, maktab faol, LEKIN mavjud dastur yo'q
    // (`docs/07` 1.1, 2026-09-03). `404` (noma'lum havola) dan ATAYIN ajratilgan.
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<GetSchoolInfoResult>> GetSchoolInfo(string slug, [FromQuery(Name = "k")] string? k, CancellationToken cancellationToken)
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
    [ProducesResponseType(typeof(StartSessionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(StartSessionResult), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<StartSessionResult>> StartSession([FromBody] StartSessionRequest request, CancellationToken cancellationToken)
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
        // yaratmaydi — REST semantikasiga ko'ra 200 tanlandi; PM'ga savol, `docs/06` §8 da
        // ikkalasi ham qonuniy deb tasdiqlandi).
        return result.Value.Resumed
            ? Ok(result.Value)
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>`GET /api/public/sessions/me` — `docs/07` 1.3-bo'lim. `X-Session-Token` bo'yicha holatni tiklaydi.</summary>
    [HttpGet("sessions/me")]
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    [ProducesResponseType(typeof(GetSessionStateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone, "application/problem+json")]
    public async Task<ActionResult<GetSessionStateResult>> GetSessionState(CancellationToken cancellationToken)
    {
        // `assessmentId` URL/tanadan emas — `SessionTokenAuthenticationHandler` autentifikatsiya
        // paytida `HttpContext.Items`ga qo'ygan (IDOR himoyasi, `CLAUDE.md` 8-qoida).
        var assessmentId = (Guid)HttpContext.Items["AssessmentId"]!;

        var result = await _sender.Send(new GetSessionStateQuery(assessmentId), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `POST /api/public/sessions/tests/{testCode}/start` — `docs/07` 1.4-bo'lim. Testni
    /// boshlaydi (aralashtirish tartibi shu yerda bir martalik qat'iylashadi).
    /// </summary>
    [HttpPost("sessions/tests/{testCode}/start")]
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    [ProducesResponseType(typeof(StartTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone, "application/problem+json")]
    public async Task<ActionResult<StartTestResult>> StartTest(string testCode, CancellationToken cancellationToken)
    {
        var assessmentId = (Guid)HttpContext.Items["AssessmentId"]!;

        var result = await _sender.Send(new StartTestCommand(assessmentId, testCode), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`GET /api/public/sessions/tests/{testCode}/questions?page=1` — `docs/07` 1.5-bo'lim.</summary>
    [HttpGet("sessions/tests/{testCode}/questions")]
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    [ProducesResponseType(typeof(GetTestQuestionsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone, "application/problem+json")]
    public async Task<ActionResult<GetTestQuestionsResult>> GetTestQuestions(string testCode, [FromQuery] int page, CancellationToken cancellationToken)
    {
        var assessmentId = (Guid)HttpContext.Items["AssessmentId"]!;

        // `page` berilmasa (query'da yo'q) `int` default'i `0` — validator `GreaterThanOrEqualTo(1)`
        // bilan `400 VALIDATION_ERROR` qaytaradi (docs'da default qiymat ko'rsatilmagan, birinchi
        // sahifani sukut bo'yicha taxmin qilish o'rniga aniq xato afzal — PM'ga savol).
        var result = await _sender.Send(new GetTestQuestionsQuery(assessmentId, testCode, page), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `POST /api/public/sessions/tests/{testCode}/answers` — `docs/07` 1.6-bo'lim. Paketli,
    /// idempotent saqlash (autosave).
    /// </summary>
    [HttpPost("sessions/tests/{testCode}/answers")]
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    [EnableRateLimiting(RateLimitSetup.PublicSaveAnswers)]
    [ProducesResponseType(typeof(SaveAnswersResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<SaveAnswersResult>> SaveAnswers(string testCode, [FromBody] SaveAnswersRequest request, CancellationToken cancellationToken)
    {
        var assessmentId = (Guid)HttpContext.Items["AssessmentId"]!;

        var command = request.ToCommand(assessmentId, testCode);
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `POST /api/public/sessions/tests/{testCode}/complete` — `docs/07` 1.7-bo'lim.
    /// `ScoringEngine` sinxron chaqiriladi, `TestResult` yoziladi.
    /// </summary>
    [HttpPost("sessions/tests/{testCode}/complete")]
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    [ProducesResponseType(typeof(CompleteTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone, "application/problem+json")]
    public async Task<ActionResult<CompleteTestResult>> CompleteTest(string testCode, CancellationToken cancellationToken)
    {
        var assessmentId = (Guid)HttpContext.Items["AssessmentId"]!;

        var result = await _sender.Send(new CompleteTestCommand(assessmentId, testCode), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `POST /api/public/sessions/complete` — `docs/07` 1.8-bo'lim. Barcha testlar tugagach
    /// yakuniy tasdiq: ishonchlilik hisoblanadi, `MaturityIndex` BIG5 natijasiga yoziladi,
    /// AI navbatga qo'yiladi (`Analyzing`). **Idempotent** — ikkinchi chaqiruv xato bermaydi.
    /// </summary>
    [HttpPost("sessions/complete")]
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    [ProducesResponseType(typeof(CompleteSessionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone, "application/problem+json")]
    public async Task<ActionResult<CompleteSessionResult>> CompleteSession(CancellationToken cancellationToken)
    {
        var assessmentId = (Guid)HttpContext.Items["AssessmentId"]!;

        var result = await _sender.Send(new CompleteSessionCommand(assessmentId), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `GET /api/public/sessions/result` — `docs/07` 1.9-bo'lim. O'quvchiga **qisqartirilgan**
    /// natija — faqat superadmin sozlamasi (`App:ShowResultToStudent`, standart `false`) yoqilgan
    /// bo'lsa. Tahlil hali tayyor bo'lmasa (`Analyzed` holatiga yetmagan — AI ulanmaguncha,
    /// P18'gacha, bu HAR DOIM shu holat) — tana yo'q `202 Accepted`.
    /// </summary>
    [HttpGet("sessions/result")]
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    [ProducesResponseType(typeof(GetStudentResultResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone, "application/problem+json")]
    public async Task<ActionResult<GetStudentResultResult>> GetStudentResult(CancellationToken cancellationToken)
    {
        var assessmentId = (Guid)HttpContext.Items["AssessmentId"]!;

        var result = await _sender.Send(new GetStudentResultQuery(assessmentId), cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return result.Value is null ? Accepted() : Ok(result.Value);
    }
}
