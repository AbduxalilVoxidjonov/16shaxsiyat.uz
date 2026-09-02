namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// AI provayder chaqiruvi natijasidagi xato turi — `docs/09-ai-analiz-moduli.md` 2-bo'lim
/// va 7-bo'limdagi retry siyosati shu qiymatlarga qarab qaror qabul qiladi:
/// `Auth` — darhol keyingi providerga (retry yo'q); `Timeout`/`RateLimit`/`Server` — 2s/6s/15s
/// kutib qayta urinish; `Schema` — tuzatuvchi eslatma bilan qayta so'rash;
/// `BadRequest` — shu providerda QAYTA URINILMAYDI (bir xil so'rov yana rad etiladi), lekin
/// zanjirdagi KEYINGI providerga o'tiladi (so'rov shakli providerlar orasida farq qiladi —
/// `prompts/17` javob xati, PM ko'rsatmasi 2026-09-02). Agar zanjirdagi HAMMA provider
/// `BadRequest` bersa — bu bizning so'rov shaklimizdagi xato, provider nosozligi emas;
/// `AiAnalysis.ErrorMessage` shuni aniq aytishi kerak (P18 mas'uliyati).
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
    BadRequest = 7,
}
