using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Api.Auth;

/// <summary>
/// `X-Session-Token` sarlavhasi bo'yicha o'quvchi sessiyasini autentifikatsiya qiladi
/// (`docs/08-auth-va-xavfsizlik.md` 4-bo'lim). Sessiyani topadi, muddatini tekshiradi va
/// `HttpContext.Items["AssessmentId"]` ga qo'yadi — IDOR himoyasi uchun `assessmentId`
/// hech qachon URL/tanadan qabul qilinmaydi (`CLAUDE.md` 8-qoida).
///
/// Muddati o'tgan sessiya uchun `401` emas `410 SESSION_EXPIRED` qaytariladi — bu standart
/// `Challenge` oqimidan chetga chiqadi, shu sabab `HandleChallengeAsync` qayta yozilib,
/// javob to'g'ridan-to'g'ri `ProblemDetails` sifatida yoziladi (`docs/06` 6-bo'lim shakli).
/// </summary>
public sealed class SessionTokenAuthenticationHandler : AuthenticationHandler<SessionTokenAuthenticationSchemeOptions>
{
    public const string SchemeName = "SessionToken";
    public const string HeaderName = "X-Session-Token";
    private const string ExpiredReasonItemKey = "SessionAuthFailureReason.SessionExpired";

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;

    public SessionTokenAuthenticationHandler(
        IOptionsMonitor<SessionTokenAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime)
        : base(options, logger, encoder)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var token = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        // P47: qidiruv XOM token ustunidan (`session_token`) SHA-256 XESHIGA ko'chirildi
        // (`Assessment.SessionTokenHash`, `TokenHash.Compute`) — refresh tokenlar bilan bir
        // xil himoya darajasi: DB nusxasi sizib chiqsa ham faol sessiyalarni bevosita ochib
        // bo'lmaydi. Xesh ustunida unikal indeks bor, ya'ni so'rov narxi o'zgarmadi.
        //
        // ⚠️ KEYINGI MIGRATSIYA (ikki bosqichli destruktiv o'zgarish, `CLAUDE.md` 7-qoida):
        // ochiq `assessments.session_token` ustuni endi HECH QAYERDA o'qilmaydi (faqat
        // `Assessment.Create`/`RotateSessionToken` yozadi va sessiya ochilganda bir marta
        // mijozga qaytariladi). Ikkinchi bosqichda alohida migratsiya bilan ustun va uning
        // domendagi xususiyati O'CHIRILISHI kerak.
        var tokenHash = TokenHash.Compute(token);

        var assessment = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.SessionTokenHash == tokenHash),
            Context.RequestAborted).ConfigureAwait(false);

        if (assessment is null)
        {
            return AuthenticateResult.Fail("Sessiya topilmadi.");
        }

        if (assessment.ExpiresAt <= _dateTime.UtcNow)
        {
            Context.Items[ExpiredReasonItemKey] = true;
            return AuthenticateResult.Fail("Sessiyaning amal qilish muddati tugagan.");
        }

        Context.Items["AssessmentId"] = assessment.Id;

        var identity = new ClaimsIdentity(
            [new Claim("assessment_id", assessment.Id.ToString())],
            SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var isExpired = Context.Items.ContainsKey(ExpiredReasonItemKey);
        var status = isExpired ? StatusCodes.Status410Gone : StatusCodes.Status401Unauthorized;
        var code = isExpired ? ProblemCodes.SessionExpired : ProblemCodes.Unauthorized;
        var title = isExpired ? "Sessiya muddati tugagan" : "Sessiya tokeni yaroqsiz.";

        Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://studentroadmap/errors/{code.ToLowerInvariant().Replace('_', '-')}",
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = Context.TraceIdentifier;

        // `WriteAsJsonAsync` `ContentType`ni o'zi qayta yozadi — shu sabab aniq shu yerda beriladi.
        return Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
