using System.Globalization;
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

    /// <summary>
    /// Himoyalangan admin API endpointlari (`SchoolsController`/`StudentsController`/
    /// `AuthController`ning `[Authorize]`li amallari) — IP bo'yicha 300/daqiqa (`docs/07`
    /// 4-bo'lim: "Admin API (umumiy) 300/daqiqa"). IP bo'yicha (foydalanuvchi emas) — chunki
    /// `UseRateLimiter()` `UseAuthentication()`dan OLDIN ishga tushadi (`Program.cs`), shu
    /// sabab JWT `sub` claim'i limiter ishlagan paytda hali mavjud emas (`AdminLogin`dagi
    /// bilan bir xil sabab/izoh). Amaliy oqibat: bir xil IP/NAT orqasidagi bir nechta admin
    /// bitta kvotani baham ko'radi — MVP uchun qabul qilingan (yagona superadmin, `docs/01`).
    /// </summary>
    public const string AdminApi = "AdminApi";

    /// <summary>
    /// `POST /api/auth/telegram` — IP bo'yicha 10/5 daqiqa, `AdminLogin` bilan AYNAN bir xil
    /// qiymat va sabab. Bu yerda hisob blokirovkasi (`AdminUser.IsLocked`) EKVIVALENTI YO'Q —
    /// Telegram imzosini "sinab ko'rish" mumkin emas (parol emas, HMAC), lekin IP limiti
    /// soxta imzo bilan bombardimon qilishning (har urinish SHA-256 + HMAC hisoblashi va
    /// audit yozuvi) oldini oladi.
    ///
    /// ⚠️ `refresh`/`logout` bu siyosatga KIRMAYDI (`PublicUserApi` ostida): ular KIRISH
    /// urinishi emas, muntazam ish trafigi (access token 30 daqiqada bir yangilanadi), va
    /// bitta NAT orqasidagi bir nechta foydalanuvchi 10/5 daqiqa kvotasini oddiy
    /// foydalanishda ham tugatib qo'yardi. `AdminLogin` ham faqat `login` ga qo'llanadi —
    /// bir xil naqsh.
    /// </summary>
    public const string PublicTelegramAuth = "PublicTelegramAuth";

    /// <summary>
    /// Ommaviy kabinet o'qish endpointlari (`GET /api/me`, `/api/me/assessments`,
    /// `/api/me/assessments/{id}/result`, `DELETE /api/me`) — IP bo'yicha 120/daqiqa.
    /// `AdminApi` (300/daqiqa) dan pastroq: kabinet trafigi ancha kam va foydalanuvchi bazasi
    /// ochiq. IP bo'yicha (foydalanuvchi emas) — `UseRateLimiter()` `UseAuthentication()`dan
    /// OLDIN ishlaydi (`Program.cs`), ya'ni JWT `sub` limiter ishlagan paytda hali yo'q
    /// (`AdminApi` bilan bir xil sabab/cheklov).
    /// </summary>
    public const string PublicUserApi = "PublicUserApi";

    /// <summary>
    /// `POST /api/public/schools/resolve-code` — IP bo'yicha 10/5 daqiqa, `AdminLogin` bilan
    /// AYNAN bir xil qattiqlik. Maktab kodi 31^8 fazoda (≈8.5·10^11) — brute-force amalda
    /// imkonsiz, lekin kod QISQA va qo'lda kiritiladigan sir; siyosat bo'lishi shart
    /// (`docs/08` 3a). Muvaffaqiyatsiz urinish audit'ga ham yoziladi — bu limit o'sha
    /// yozuvlar hajmini ham cheklaydi.
    /// </summary>
    public const string PublicResolveSchoolCode = "PublicResolveSchoolCode";

    /// <summary>Rad etilgan so'rov uchun oyna uzunligi saqlanadigan `HttpContext.Items` kaliti.</summary>
    private const string RetryAfterWindowItemKey = "RateLimit.RetryAfterWindow";

    /// <summary>Oyna uzunligi ham, liz metadatasi ham topilmasa (kutilmagan holat) — xavfsiz standart.</summary>
    private static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                // `Retry-After` (RFC 9110 10.2.3) — foydalanuvchi/klient qancha kutishini
                // BILISHI kerak (P31 topilmasi: 429 javobida bu sarlavha yo'q edi va mijoz
                // "birozdan so'ng" degan mavhum matndan boshqa hech narsa ololmasdi).
                // Sekundlarda, butun songa YUQORIGA yaxlitlanadi — 0 qaytarib mijozni darhol
                // qayta urinishga undamaslik uchun (kamida 1).
                var retryAfter = ResolveRetryAfter(context);
                var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

                context.HttpContext.Response.Headers.RetryAfter =
                    retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "So'rovlar soni limitdan oshdi.",
                    Detail = $"Iltimos, {retryAfterSeconds} soniyadan so'ng qayta urinib ko'ring.",
                    Type = "https://studentroadmap/errors/rate-limited",
                };
                problem.Extensions["code"] = ProblemCodes.RateLimited;
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                // Sarlavhani o'qiy olmaydigan klient (masalan `fetch` CORS'da ochilmagan
                // sarlavhalar) uchun bir xil qiymat tanada ham beriladi.
                problem.Extensions["retryAfterSeconds"] = retryAfterSeconds;

                // `WriteAsJsonAsync` `ContentType`ni o'zi qayta yozadi — shu sabab aniq shu yerda beriladi.
                await context.HttpContext.Response
                    .WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken)
                    .ConfigureAwait(false);
            };

            options.AddPolicy(PublicSchoolInfo, httpContext => FixedWindow(
                httpContext,
                partitionKey: GetClientIp(httpContext),
                permitLimit: 60,
                window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(PublicStartSession, httpContext => FixedWindow(
                httpContext,
                partitionKey: GetClientIp(httpContext),
                permitLimit: 10,
                window: TimeSpan.FromHours(1)));

            // Sessiya (`X-Session-Token`) bo'yicha bo'linadi — IP emas, chunki bitta maktab
            // kompyuter sinfida bir nechta o'quvchi bitta IP orqasida bo'lishi mumkin
            // (`docs/07` 4-bo'lim: "sessiya bo'yicha 120/daqiqa"). Autentifikatsiya (`UseAuthentication`)
            // `UseRateLimiter`dan KEYIN ishga tushadi (`Program.cs`), shu sabab bu yerda xom
            // sarlavha qiymati ishlatiladi — token yaroqsiz bo'lsa ham partitsiya kaliti sifatida
            // yetarli (keyinroq autentifikatsiya 401/410 bilan rad etadi).
            options.AddPolicy(AdminLogin, httpContext => FixedWindow(
                httpContext,
                partitionKey: GetClientIp(httpContext),
                permitLimit: 10,
                window: TimeSpan.FromMinutes(5)));

            options.AddPolicy(PublicSaveAnswers, httpContext => FixedWindow(
                httpContext,
                partitionKey: GetSessionToken(httpContext),
                permitLimit: 120,
                window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(PublicResolveSchoolCode, httpContext => FixedWindow(
                httpContext,
                partitionKey: GetClientIp(httpContext),
                permitLimit: 10,
                window: TimeSpan.FromMinutes(5)));

            options.AddPolicy(PublicTelegramAuth, httpContext => FixedWindow(
                httpContext,
                partitionKey: GetClientIp(httpContext),
                permitLimit: 10,
                window: TimeSpan.FromMinutes(5)));

            options.AddPolicy(PublicUserApi, httpContext => FixedWindow(
                httpContext,
                partitionKey: GetClientIp(httpContext),
                permitLimit: 120,
                window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(AdminApi, httpContext => FixedWindow(
                httpContext,
                partitionKey: GetClientIp(httpContext),
                permitLimit: 300,
                window: TimeSpan.FromMinutes(1)));
        });

        return services;
    }

    /// <summary>
    /// Qat'iy oyna (fixed window) limiterini yaratadi va SHU BILAN BIRGA oyna uzunligini
    /// `HttpContext.Items`ga qo'yadi. Sabab: `OnRejected` global (siyosatga bog'liq emas) va
    /// rad etilgan so'rov qaysi siyosatga tegishli ekanini bilmaydi — `Retry-After` qiymatini
    /// hisoblash uchun esa oyna uzunligi kerak. Siyosat delegati HAR so'rovda chaqiriladi,
    /// shu sabab bu yer qiymatni saqlash uchun ishonchli joy. Limiter lizingi metadata
    /// (`MetadataName.RetryAfter`) bersa — u ustunlik qiladi (aniqroq: oynaning QOLGAN qismi).
    /// </summary>
    private static RateLimitPartition<string> FixedWindow(
        HttpContext httpContext,
        string partitionKey,
        int permitLimit,
        TimeSpan window)
    {
        httpContext.Items[RetryAfterWindowItemKey] = window;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
            });
    }

    private static TimeSpan ResolveRetryAfter(OnRejectedContext context)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var leaseRetryAfter) && leaseRetryAfter > TimeSpan.Zero)
        {
            return leaseRetryAfter;
        }

        if (context.HttpContext.Items.TryGetValue(RetryAfterWindowItemKey, out var stored) && stored is TimeSpan window)
        {
            return window;
        }

        return DefaultRetryAfter;
    }

    private static string GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string GetSessionToken(HttpContext httpContext) =>
        httpContext.Request.Headers.TryGetValue(SessionTokenAuthenticationHandler.HeaderName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : "unknown";
}
