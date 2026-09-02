namespace StudentRoadMap.Api.Auth;

/// <summary>
/// Refresh token cookie sozlamalari — `docs/08-auth-va-xavfsizlik.md` 2-bo'lim:
/// "`httpOnly; Secure; SameSite=Strict` cookie'da". `Path` faqat `/api/auth` bilan
/// cheklangan — cookie boshqa endpointlarga (masalan, admin CRUD) yuborilmaydi, XSS/CSRF
/// yuzasini kamaytiradi.
/// </summary>
internal static class RefreshTokenCookie
{
    public const string Name = "srm_refresh_token";
    public const string Path = "/api/auth";

    public static CookieOptions BuildOptions(DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
    };

    public static CookieOptions BuildDeleteOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
    };
}
