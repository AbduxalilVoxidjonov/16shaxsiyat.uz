namespace StudentRoadMap.Api.Auth;

/// <summary>
/// Ommaviy (Telegram) foydalanuvchining refresh token cookie'si — `RefreshTokenCookie`
/// (superadmin) naqshini AYNAN takrorlaydi (`httpOnly; Secure; SameSite=Strict`,
/// `docs/08` 2-bo'lim), lekin IKKI narsasi ATAYLAB boshqacha:
///
/// • <see cref="Name"/> — superadminникidan farqli (`srm_refresh_token` EMAS). Bir xil nom
///   bo'lsa, bitta brauzerda superadmin panelidan ham, ommaviy kabinetdan ham foydalanish
///   cookie'lar bir-birini ustidan yozib, ikkala sessiyani ham jimgina buzardi.
/// • <see cref="Path"/> — `/api/auth/telegram` (superadminники `/api/auth`). Brauzer
///   cookie'ni faqat shu prefiksli so'rovlarga qo'shadi, ya'ni ommaviy refresh token
///   superadmin endpointlariga (va `/api/me/*` ga) UMUMAN yuborilmaydi — XSS/CSRF yuzasi
///   minimal qoladi. `refresh` va `logout` ikkalasi ham shu prefiks ostida.
/// </summary>
internal static class PublicRefreshTokenCookie
{
    public const string Name = "srm_public_refresh_token";
    public const string Path = "/api/auth/telegram";

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
