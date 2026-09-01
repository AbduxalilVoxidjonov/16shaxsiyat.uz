using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        });

        return services;
    }

    private static string GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
