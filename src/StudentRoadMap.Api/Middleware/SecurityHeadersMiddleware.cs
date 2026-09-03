namespace StudentRoadMap.Api.Middleware;

/// <summary>
/// `docs/08-auth-va-xavfsizlik.md` 7-bo'limidagi HTTP xavfsizlik sarlavhalarini API
/// javoblariga qo'shadi. HSTS bu yerda EMAS — u `app.UseHsts()` (faqat Production) orqali,
/// `Program.cs`da sozlanadi.
///
/// **Nima uchun `OnStarting`.** `UseExceptionHandler` ushlanmagan istisnodan keyin javobni
/// TOZALAYDI (`Response.Clear()` — status va BARCHA sarlavhalar) va handler'ni qaytadan
/// ishga tushiradi. Sarlavhalar shu middleware'ning o'zida darhol qo'yilsa, aynan xato
/// javoblarida (ya'ni eng muhim holatda) yo'qolib ketardi. `OnStarting` qayta chaqiruvi esa
/// javob HAQIQATDA yozila boshlaganda ishlaydi va tozalashdan keyin ham saqlanib qoladi.
///
/// **CSP.** API faqat JSON/fayl qaytaradi — hech qanday skript yoki stil bermaydi, shu sabab
/// eng qattiq siyosat: `default-src 'none'`. Frontendning (nginx, `docker/web-nginx.conf`)
/// CSP'si undan boshqacha — u haqiqiy sahifa beradi. Swagger UI (faqat Development) inline
/// skript va stildan foydalanadi, shu sabab `/swagger` yo'llariga CSP QO'YILMAYDI — aks holda
/// hujjat sahifasi oq ekranga aylanardi. Production'da Swagger umuman yoqilmaydi.
/// `unsafe-eval` hech bir siyosatda yo'q (`prompts/31` cheklovi).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    /// <summary>API javoblari uchun: hech qanday resurs yuklanmaydi, sahifaga joylab bo'lmaydi.</summary>
    internal const string ApiContentSecurityPolicy =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    internal const string ReferrerPolicy = "strict-origin-when-cross-origin";
    internal const string PermissionsPolicy = "geolocation=(), microphone=(), camera=()";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var httpContext = (HttpContext)state;
            var headers = httpContext.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = ReferrerPolicy;
            headers["Permissions-Policy"] = PermissionsPolicy;

            if (!IsSwaggerPath(httpContext.Request.Path))
            {
                headers["Content-Security-Policy"] = ApiContentSecurityPolicy;
            }

            return Task.CompletedTask;
        }, context);

        return _next(context);
    }

    private static bool IsSwaggerPath(PathString path) =>
        path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
}

/// <summary>`SecurityHeadersMiddleware`ni quvurga ulash uchun kengaytma.</summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Quvurning ENG BOSHIDA (`UseConfiguredForwardedHeaders`dan keyin) chaqiriladi — shunda
    /// undan keyingi HAR QANDAY javob (xato, 404, rate limit, autentifikatsiya rad javobi)
    /// sarlavhalarni oladi.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
