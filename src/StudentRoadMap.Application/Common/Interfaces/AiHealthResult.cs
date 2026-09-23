namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// `IAiAnalysisProvider.CheckHealthAsync` javobi — superadmin panelida "ulanishni tekshirish"
/// (`docs/04-domain-model.md` 2.9-bo'lim: `AiProviderConfig.LastCheckStatus`/`LastCheckedAt`) uchun.
/// </summary>
/// <param name="IsHealthy">Provider hozir ishlayaptimi (kalit to'g'ri, tarmoq bor).</param>
/// <param name="Message">Qisqa holat tavsifi — API kalitini o'z ichiga olmaydi (`AiHttpExecutor.Redact` bilan tozalangan).</param>
/// <param name="ErrorKind">
/// P18 (`Application.Admin.Ai.TestProvider`): `IsHealthy = false` bo'lganda xato turi — chaqiruvchi
/// (masalan admin "ulanishni tekshirish" endpointi) shu bo'yicha ANIQ, harakatga yo'naltiruvchi
/// o'zbekcha xabar tuzadi ("API kaliti noto'g'ri" kabi — provayder xom javobi TO'G'RIDAN-TO'G'RI
/// foydalanuvchiga ko'rsatilmasligi kerak). `IsHealthy = true` bo'lganda har doim `None`.
/// </param>
/// <param name="StatusCode">Provayder qaytargan HTTP status kodi (javob bo'lmasa — timeout/tarmoq — `null`).
/// Masalan 503 ("model overloaded") va boshqa 5xx ni admin uchun farqlash uchun.</param>
/// <param name="ProviderDetail">
/// Provayder javob tanasidagi `error.message` — qisqartirilgan, API kaliti va kalitga o'xshash
/// satrlar olib tashlangan (`AiHttpExecutor.ExtractSafeDetail`). Butun xom tana EMAS.
/// </param>
/// <param name="Model">Sinalgan model nomi — "model topilmadi" xabarida ko'rsatish uchun.</param>
public sealed record AiHealthResult(
    bool IsHealthy,
    string? Message,
    AiErrorKind ErrorKind = AiErrorKind.None,
    int? StatusCode = null,
    string? ProviderDetail = null,
    string? Model = null);
