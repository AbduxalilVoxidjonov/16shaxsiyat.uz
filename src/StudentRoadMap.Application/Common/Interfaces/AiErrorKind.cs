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

    /// <summary>
    /// Model nomi provayderda topilmadi (404, yoki tanasida `model ... not found` /
    /// `model_not_found` bo'lgan 4xx). `BadRequest` bilan bir xil retry siyosati — shu
    /// providerda qayta urinish befoyda (model nomi o'zgarmaydi), lekin zanjirdagi keyingi
    /// providerga o'tiladi. Admin uchun xabar boshqacha: "model topilmadi — model nomini
    /// tekshiring" (P28 koordinator ko'rsatmasi, 2026-09-02).
    /// </summary>
    ModelNotFound = 8,

    /// <summary>
    /// Provayderga umuman ulanib bo'lmadi (DNS, TLS, `HttpRequestException`) — HTTP javob YO'Q.
    /// `Timeout`/`Server` kabi qayta urinishga arziydi; admin uchun xabar "tarmoq" haqida.
    /// </summary>
    Network = 9,
}
