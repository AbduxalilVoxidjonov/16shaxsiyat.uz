namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// `IAiAnalysisProvider.CompleteJsonAsync` javobi — `docs/09-ai-analiz-moduli.md` 2-bo'lim.
/// `Success = false` bo'lganda `ErrorKind` retry/fallback siyosatini boshqaradi (7-bo'lim).
/// </summary>
/// <param name="Success">So'rov muvaffaqiyatli bajarildimi (tarmoq/auth darajasida — JSON schema
/// va taqiqlangan atamalar tekshiruvi bu yerda emas, `AiResponseValidator`da).</param>
/// <param name="RawJson">Providerdan qaytgan xom JSON matni (muvaffaqiyatsizlikda `null` bo'lishi mumkin).</param>
/// <param name="InputTokens">Kirish token soni (xarajat hisobi uchun, `docs/09` 9-bo'lim).</param>
/// <param name="OutputTokens">Chiqish token soni.</param>
/// <param name="DurationMs">So'rov davomiyligi, millisekundda.</param>
/// <param name="ErrorMessage">Xato tavsifi (API kaliti va boshqa sirlarni HECH QACHON o'z ichiga olmaydi — `CLAUDE.md`/`docs/09` 7-bo'lim).</param>
/// <param name="ErrorKind">Xato turi — `None` muvaffaqiyat.</param>
public sealed record AiCompletionResult(
    bool Success,
    string? RawJson,
    int? InputTokens,
    int? OutputTokens,
    int DurationMs,
    string? ErrorMessage,
    AiErrorKind ErrorKind);
