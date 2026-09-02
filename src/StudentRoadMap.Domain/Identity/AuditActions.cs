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

    // --- P14 (`prompts/14-admin-maktab-va-oquvchi-api.md`) — `docs/08` 8-bo'limida ro'yxat
    // qilingan maktab/o'quvchi harakatlari. ---

    public const string SchoolCreated = "School.Created";
    public const string SchoolUpdated = "School.Updated";
    public const string SchoolDeleted = "School.Deleted";
    public const string SchoolLinkRegenerated = "School.LinkRegenerated";
    public const string SchoolToggledActive = "School.ToggledActive";
    public const string StudentDeleted = "Student.Deleted";
}
