namespace StudentRoadMap.Application.PublicUsers.Common;

/// <summary>
/// Ommaviy foydalanuvchi harakatlari uchun `AuditLog.Action` qiymatlari (`docs/08` 8-bo'lim).
///
/// **Nima uchun `Domain.Identity.AuditActions` ichida EMAS:** domen qatlami bu bosqichda
/// qulflangan (P47 domen agenti yakunlagan, `TEGMA`). Qiymatlar AYNAN o'sha uslubda
/// (`Sohа.Harakat`) yozilgan va bir xil `audit_logs` jadvaliga tushadi — kelajakda domen
/// qatlami ochilganda `AuditActions` ichiga ko'chirilishi mumkin, satr qiymatlari
/// o'zgarmagani uchun bu bazadagi tarixni buzmaydi.
///
/// ⚠️ `AuditLog.AdminUserId` ommaviy oqimda HAR DOIM `null` — u `admin_users` ga FK
/// (`AuditLogConfiguration`), ommaviy foydalanuvchi esa boshqa jadvalda. Kim ekanligi
/// `EntityType = "PublicUser"` + `EntityId = publicUserId` orqali yoziladi
/// (`ix_audit_logs_entity` indeksi aynan shu juftlik bo'yicha).
/// </summary>
public static class PublicAuditActions
{
    /// <summary>`audit_logs.entity_type` qiymati — ommaviy foydalanuvchi yozuvlari uchun.</summary>
    public const string PublicUserEntityType = "PublicUser";

    /// <summary>`POST /api/auth/telegram` muvaffaqiyatli — akkaunt yaratildi yoki kirildi.</summary>
    public const string LoginSucceeded = "PublicAuth.LoginSucceeded";

    /// <summary>
    /// `POST /api/auth/telegram` rad etildi (imzo noto'g'ri yoki `auth_date` eskirgan).
    /// `EntityId` YOZILMAYDI — imzo tekshiruvdan o'tmagan `id` ga ishonib bo'lmaydi.
    /// </summary>
    public const string LoginFailed = "PublicAuth.LoginFailed";

    /// <summary>`DELETE /api/me` — foydalanuvchi o'z akkauntini o'chirdi (anonimlashtirish).</summary>
    public const string AccountDeleted = "PublicUser.Deleted";

    /// <summary>
    /// Bekor qilingan refresh token bilan qayta urinish — o'g'irlik belgisi
    /// (`Security.RefreshReuse` superadmin ekvivalenti, `docs/13` MAXSUS DIQQAT 3-band).
    /// </summary>
    public const string RefreshReuse = "PublicSecurity.RefreshReuse";
}
