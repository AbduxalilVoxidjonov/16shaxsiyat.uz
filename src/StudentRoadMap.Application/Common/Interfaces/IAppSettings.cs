namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Sir bo'lmagan, konfiguratsiyadan olinadigan ilova sozlamalari (`docs/06-arxitektura.md`
/// 7-bo'lim). Sirlar (`Jwt:Key`, `Security:EncryptionKey` va h.k.) bu orqali berilmaydi —
/// ular alohida, maxsus servislar (`IEncryptionService` va h.k.) orqali o'qiladi.
/// </summary>
public interface IAppSettings
{
    /// <summary>O'quvchi sessiya tokenining amal qilish muddati, kunlarda (`docs/08` 4-bo'lim: 7 kun).</summary>
    int SessionLifetimeDays { get; }

    /// <summary>
    /// O'quvchiga qisqartirilgan natijani ko'rsatish (`GET /api/public/sessions/result`,
    /// `docs/07` 1.9-bo'lim) yoqilganmi. Standart qiymat — **`false`** (`prompts/12`
    /// cheklovi): bu `PROGRESS.md` ochiq savol #1 — loyiha egasidan hali javob kelmagan,
    /// shuning uchun ehtiyotkor sozlama tanlangan, superadmin `App:ShowResultToStudent`
    /// orqali yoqadi.
    /// </summary>
    bool ShowResultToStudent { get; }

    /// <summary>
    /// Superadmin refresh tokenining amal qilish muddati, kunlarda (`docs/08` 2-bo'lim,
    /// `docs/06` 7-bo'lim: `Jwt:RefreshTokenDays`, standart 14). Sir emas — `Jwt:Key`dan farqli
    /// o'laroq bu shu interfeys orqali beriladi.
    /// </summary>
    int RefreshTokenDays { get; }
}
