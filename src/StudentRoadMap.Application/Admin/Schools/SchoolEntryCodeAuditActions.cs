namespace StudentRoadMap.Application.Admin.Schools;

/// <summary>
/// Maktab kodi (`School.EntryCode`) bilan bog'liq `AuditLog.Action` qiymatlari (`docs/08`
/// 8-bo'lim). `Domain.Identity.AuditActions` ichida EMAS — `PublicAuditActions` bilan bir
/// xil sabab: bu ish `Domain/Schools/**` bilan cheklangan, `Identity` papkasiga tegilmaydi.
/// Qiymatlar o'sha uslubda (`Soha.Harakat`) va bir xil `audit_logs` jadvaliga tushadi.
/// </summary>
public static class SchoolEntryCodeAuditActions
{
    /// <summary>`POST /api/admin/schools/{id}/regenerate-entry-code` — eski kod bekor bo'ldi. Kod QIYMATI yozilmaydi (`School.LinkRegenerated` bilan bir xil).</summary>
    public const string Regenerated = "School.EntryCodeRegenerated";

    /// <summary>
    /// `POST /api/public/schools/resolve-code` rad etildi — kod topilmadi/nofaol/o'chirilgan/
    /// ommaviy makon. `Auth.LoginFailed`/`PublicAuth.LoginFailed` bilan bir xil ruhda: IP xeshi
    /// va User-Agent yoziladi, KIRITILGAN KOD YOZILMAYDI (u haqiqiy kodning "yaqin xatosi"
    /// bo'lishi mumkin), `EntityId` yo'q (maktab aniqlanmagan). Hajm `RateLimitSetup.
    /// PublicResolveSchoolCode` (10/5 daqiqa/IP) bilan cheklangan.
    /// </summary>
    public const string ResolveFailed = "SchoolCode.ResolveFailed";
}
