namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// AI provayder chaqiruvi natijasidagi xato turi — `docs/09-ai-analiz-moduli.md` 2-bo'lim
/// va 7-bo'limdagi retry siyosati shu qiymatlarga qarab qaror qabul qiladi:
/// `Auth` — darhol keyingi providerga (retry yo'q); `Timeout`/`RateLimit`/`Server` — 2s/6s/15s
/// kutib qayta urinish; `Schema` — tuzatuvchi eslatma bilan qayta so'rash.
/// </summary>
public enum AiErrorKind
{
    None = 0,
    Auth = 1,
    RateLimit = 2,
    Timeout = 3,
    Schema = 4,
    Server = 5,
    Unknown = 6,
}
