namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Joriy so'rov egasi haqida abstraksiya. Superadmin oqimida (`Bearer` JWT, `docs/08` 2-bo'lim)
/// `AdminUserId`/`Role` to'ldiriladi; ommaviy (o'quvchi) oqimida sessiya identifikatori
/// `HttpContext.Items["AssessmentId"]` orqali alohida uzatiladi (IDOR himoyasi, `docs/08` 4-bo'lim) —
/// bu interfeys shu sabab faqat admin autentifikatsiyasi uchun ishlatiladi.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? AdminUserId { get; }

    string? Role { get; }
}
