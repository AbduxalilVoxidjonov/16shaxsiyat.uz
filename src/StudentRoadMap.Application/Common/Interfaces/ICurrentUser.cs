namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Joriy so'rov egasi haqida abstraksiya. Uch xil kirish modeli bor (`docs/08` 1-bo'lim):
///
/// • superadmin (`Bearer` JWT, `docs/08` 2-bo'lim) → <see cref="AdminUserId"/>/<see cref="Role"/>;
/// • ommaviy foydalanuvchi (`PublicBearer` JWT, P47) → <see cref="PublicUserId"/>;
/// • o'quvchi sessiyasi (`X-Session-Token`) → BU YERDA YO'Q: sessiya identifikatori
///   `HttpContext.Items["AssessmentId"]` orqali alohida uzatiladi (IDOR himoyasi, `docs/08` 4-bo'lim).
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? AdminUserId { get; }

    string? Role { get; }

    /// <summary>
    /// Ommaviy (Telegram) foydalanuvchi identifikatori — FAQAT `role = PublicUser` claim'li
    /// tokenda to'ldiriladi. Superadmin tokenida har doim `null`: ikkala oqim ham `sub`
    /// claim'ini ishlatadi, shu sabab rol tekshiruvisiz superadmin `sub`i ommaviy
    /// identifikator sifatida talqin qilinib ketishi mumkin edi.
    /// </summary>
    Guid? PublicUserId { get; }
}
