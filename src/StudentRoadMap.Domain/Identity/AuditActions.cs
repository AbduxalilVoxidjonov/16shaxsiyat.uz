namespace StudentRoadMap.Domain.Identity;

/// <summary>
/// `AuditLog.Action` uchun qat'iy qiymatlar ro'yxati — `docs/08-auth-va-xavfsizlik.md`
/// 8-bo'limidagi auth bilan bog'liq harakatlar. `Auth.TotpDisabled` — `EnableTotp` bilan
/// simmetrik (`docs/13-auth-va-jwt.md` DisableTotp use-case'i talab qiladi), PM tomonidan
/// `docs/08`ga rasman kiritilgan.
/// </summary>
public static class AuditActions
{
    public const string AuthLoginSucceeded = "Auth.LoginSucceeded";
    public const string AuthLoginFailed = "Auth.LoginFailed";
    public const string AuthPasswordChanged = "Auth.PasswordChanged";
    public const string AuthTotpEnabled = "Auth.TotpEnabled";
    public const string AuthTotpDisabled = "Auth.TotpDisabled";
    public const string SecurityRefreshReuse = "Security.RefreshReuse";
}
