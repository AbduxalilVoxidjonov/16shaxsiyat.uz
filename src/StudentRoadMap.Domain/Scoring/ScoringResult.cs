namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Scoring strategiyasining natijasi — `docs/06-arxitektura.md` §8 kontrakti (`ScoringVersion`
/// qo'shimchasi PM tomonidan tasdiqlangan, hujjat yangilanadi).
/// `ResultCode` faqat `MBTI16` ("INTJ") va `RIASEC` ("IRA") uchun bor, boshqalarda `null`.
/// </summary>
/// <param name="ResultCode">4 harfli tip kodi | 3 harfli Holland kodi | `null`.</param>
/// <param name="RawScores">Shkala kodi → xom ball (masalan `axisRaw`, `factorRaw`).</param>
/// <param name="NormalizedScores">Shkala kodi → 0..100 normalizatsiya qilingan foiz.</param>
/// <param name="Levels">Shkala/agregat kodi → daraja matni yoki tanlangan harf.</param>
/// <param name="CompositeIndex">`MaturityIndex` yoki `ActivityIndex` — boshqalarda `null`.</param>
/// <param name="Flags">Ogohlantirish bayroqlari (masalan `"Borderline:EI"`, `"LowDifferentiation"`).</param>
/// <param name="InterpretationKey">Lokalizatsiya kaliti.</param>
/// <param name="ScoringVersion">Formula versiyasi (`ScoringConstants.CurrentScoringVersion`) —
/// `TestResult.ScoringVersion`ga shu yerdan o'tkaziladi (`docs/03` §8: "Formula o'zgarsa
/// `TestDefinition.Version` oshiriladi... `TestResult.ScoringVersion`da qaysi versiya
/// ishlatilgani qoladi").</param>
public sealed record ScoringResult(
    string? ResultCode,
    IReadOnlyDictionary<string, double> RawScores,
    IReadOnlyDictionary<string, double> NormalizedScores,
    IReadOnlyDictionary<string, string> Levels,
    double? CompositeIndex,
    IReadOnlyList<string> Flags,
    string InterpretationKey,
    int ScoringVersion = ScoringConstants.CurrentScoringVersion);
