namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Scoring strategiyasiga kiruvchi ma'lumot — `docs/06-arxitektura.md` §8 kontrakti.
/// Sof qiymat: DB, HTTP yoki vaqtga bog'liqlik yo'q.
/// </summary>
/// <param name="Questions">Testga tegishli savollar (`Scale`, `Direction`, `Weight`, ...).</param>
/// <param name="Answers">`QuestionId → xom javob qiymati`.</param>
/// <param name="Durations">`QuestionId → javob berish davomiyligi (ms)`.</param>
/// <param name="Student">Shaxsiy bo'lmagan o'quvchi konteksti.</param>
/// <param name="ScaleBands">
/// Faqat `SUM` strategiyasi uchun: `shkala kodi → talqin oraliqlari`. Boshqa strategiyalar
/// e'tiborsiz qoldiradi (docs/06 §8 asosiy 4 maydonga qo'shimcha, chunki `SUM` shkala
/// talqinlari testga xos konfiguratsiya — `docs/03` §6.1, hali P33'gacha `TestScale` entity
/// mavjud emasligi sababli shu yerda uzatiladi; PM'ga savollar bo'limiga qarang).
/// </param>
public sealed record ScoringInput(
    IReadOnlyList<QuestionMeta> Questions,
    IReadOnlyDictionary<Guid, int> Answers,
    IReadOnlyDictionary<Guid, int> Durations,
    StudentContext Student,
    IReadOnlyDictionary<string, IReadOnlyList<InterpretationBand>>? ScaleBands = null);
