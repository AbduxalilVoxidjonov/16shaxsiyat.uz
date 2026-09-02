using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Extensions;

/// <summary>
/// Ommaviy oqim uchun IP-asosli tezlik cheklovlari — `docs/07-api-shartnoma.md` 4-bo'lim jadvali.
/// Superadmin/login siyosatlari (P13+) keyingi promptlarda shu yerga qo'shiladi.
/// </summary>
public static class RateLimitSetup
{
    /// <summary>`GET /api/public/schools/{slug}` — IP bo'yicha 60/daqiqa.</summary>
    public const string PublicSchoolInfo = "PublicSchoolInfo";

    /// <summary>`POST /api/public/sessions` — IP bo'yicha 10/soat.</summary>
    public const string PublicStartSession = "PublicStartSession";

    /// <summary>`POST /api/public/.../answers` — SESSIYA bo'yicha (IP emas) 120/daqiqa (`docs/07` 4-bo'lim).</summary>
    public const string PublicSaveAnswers = "PublicSaveAnswers";

    /// <summary>
    /// `POST /api/auth/login` — IP bo'yicha 10/5 daqiqa. Ikkinchi qatlam himoya (`docs/13-auth-va-jwt.md`
    /// MAXSUS DIQQAT 9-band): hisob blokirovkasi (5 urinish/15 daq, `AdminUser`) FOYDALANUVCHI
    /// nomiga bog'liq, bu limit esa IP manzilga — turli username'larni sinab ko'radigan
    /// (username enumeration) hujumdan ham himoya qiladi.
    /// </summary>
    public const string AdminLogin = "AdminLogin";

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "So'rovlar soni limitdan oshdi.",
                    Detail = "Iltimos, birozdan so'ng qayta urinib ko'ring.",
                    Type = "https://studentroadmap/errors/rate-limited",
                };
                problem.Extensions["code"] = ProblemCodes.RateLimited;
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                // `WriteAsJsonAsync` `ContentType`ni o'zi qayta yozadi — shu sabab aniq shu yerda beriladi.
                await context.HttpContext.Response
                    .WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken)
                    .ConfigureAwait(false);
            };

            options.AddPolicy(PublicSchoolInfo, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetClientIp(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

            options.AddPolicy(PublicStartSession, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetClientIp(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                }));

            // Sessiya (`X-Session-Token`) bo'yicha bo'linadi — IP emas, chunki bitta maktab
            // kompyuter sinfida bir nechta o'quvchi bitta IP orqasida bo'lishi mumkin
            // (`docs/07` 4-bo'lim: "sessiya bo'yicha 120/daqiqa"). Autentifikatsiya (`UseAuthentication`)
            // `UseRateLimiter`dan KEYIN ishga tushadi (`Program.cs`), shu sabab bu yerda xom
            // sarlavha qiymati ishlatiladi — token yaroqsiz bo'lsa ham partitsiya kaliti sifatida
            // yetarli (keyinroq autentifikatsiya 401/410 bilan rad etadi).
            options.AddPolicy(AdminLogin, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetClientIp(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                }));

            options.AddPolicy(PublicSaveAnswers, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetSessionToken(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        return services;
    }

    private static string GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string GetSessionToken(HttpContext httpContext) =>
        httpContext.Request.Headers.TryGetValue(SessionTokenAuthenticationHandler.HeaderName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : "unknown";
}
