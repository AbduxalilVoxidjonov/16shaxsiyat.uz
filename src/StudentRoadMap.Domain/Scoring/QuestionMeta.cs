using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Scoring uchun kerakli savol metama'lumoti — `docs/06-arxitektura.md` §8 kontraktidagi
/// `QuestionMeta` (izohda "Scale, Direction, Weight" deb ko'rsatilgan). `QuestionId` javoblar
/// lug'atidan (`ScoringInput.Answers`) qiymatni topish uchun, `QuestionType` `SUM` strategiyasida
/// javob diapazonini (`min`/`max`) aniqlash uchun, `DisplayOrder` esa ishonchlilik hisobida
/// ketma-ketlikni (straight-lining) aniqlash uchun kerak.
/// </summary>
/// <param name="QuestionId">`ScoringInput.Answers`/`Durations` lug'atidagi kalit.</param>
/// <param name="Code">Savol kodi (masalan `BIG5-Q17`) — diagnostika/xato xabarlari uchun.</param>
/// <param name="Scale">Shkala kodi — `docs/03` §1 yagona ro'yxatidan.</param>
/// <param name="Direction">`+1` yoki `-1` — teskari savol yo'nalishi.</param>
/// <param name="Weight">Og'irlik — default `1.0`, `SUM` strategiyasida ixtiyoriy og'irlik.</param>
/// <param name="QuestionType">Javob turi — `SUM` strategiyasida `min`/`max` diapazonini beradi.</param>
/// <param name="DisplayOrder">Ko'rsatish tartibi — ishonchlilik (straight-lining) ketma-ketligi uchun.</param>
public sealed record QuestionMeta(
    Guid QuestionId,
    string Code,
    string Scale,
    int Direction,
    decimal Weight,
    QuestionType QuestionType,
    int DisplayOrder);
