using System.Text.Json.Serialization;

namespace StudentRoadMap.Infrastructure.Ai;

/// <summary>
/// AI'ga yuboriladigan strukturalangan kirish — `docs/09-ai-analiz-moduli.md` 3-bo'lim.
/// <para>
/// **Shaxsiy ma'lumot YO'Q**: ism, telefon, email, aniq tug'ilgan sana, maktab nomi bu yerda
/// hech qachon bo'lmaydi (`CLAUDE.md` 5-qoida, `docs/08-auth-va-xavfsizlik.md` 5-bo'lim) —
/// faqat yosh (butun son), sinf, jins va ballar/indekslar. `PromptBuilderTests`dagi majburiy
/// maxfiylik testi shuni tekshiradi.
/// </para>
/// Har bir maydon `JsonPropertyName` bilan aniq belgilangan — chiqish JSON'i `docs/09` §3
/// misoliga so'zma-so'z mos bo'lishi uchun (masalan `bigFive.O`/`bigFive.C` katta harf bilan,
/// qolganlari camelCase — global naming policy bunga yetarli emas).
/// </summary>
internal sealed record AnalysisInput(
    [property: JsonPropertyName("context")] AnalysisContext Context,
    [property: JsonPropertyName("reliability")] AnalysisReliability Reliability,
    [property: JsonPropertyName("personality16")] AnalysisPersonality16? Personality16,
    [property: JsonPropertyName("bigFive")] AnalysisBigFive? BigFive,
    [property: JsonPropertyName("interests")] AnalysisInterests? Interests,
    [property: JsonPropertyName("activity")] AnalysisActivity? Activity,
    [property: JsonPropertyName("customTests")] IReadOnlyList<AnalysisCustomTest> CustomTests);

/// <summary>Ruxsat etilgan yagona "shaxsiy" maydonlar — yosh (butun son), sinf, jins, til (`docs/09` §3).</summary>
internal sealed record AnalysisContext(
    [property: JsonPropertyName("age")] int Age,
    [property: JsonPropertyName("grade")] int Grade,
    [property: JsonPropertyName("gender")] string Gender,
    [property: JsonPropertyName("language")] string Language);

internal sealed record AnalysisReliability(
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("flag")] string Flag,
    [property: JsonPropertyName("notes")] IReadOnlyList<string> Notes);

internal sealed record AnalysisAxis(
    [property: JsonPropertyName("pct")] double Pct,
    [property: JsonPropertyName("letter")] string Letter);

internal sealed record AnalysisPersonality16(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("typeNameUz")] string TypeNameUz,
    [property: JsonPropertyName("axes")] IReadOnlyDictionary<string, AnalysisAxis> Axes,
    [property: JsonPropertyName("borderlineAxes")] IReadOnlyList<string> BorderlineAxes);

internal sealed record AnalysisFactorLevel(
    [property: JsonPropertyName("pct")] double Pct,
    [property: JsonPropertyName("level")] string Level);

/// <summary>
/// `N` (Nevrotizm) qasddan YO'Q — `docs/09` §3 namunasida faqat `O/C/E/A` + ijobiy talqin
/// qilingan `stability` (`= 100 − N`) ko'rsatiladi.
/// </summary>
internal sealed record AnalysisBigFive(
    [property: JsonPropertyName("O")] AnalysisFactorLevel O,
    [property: JsonPropertyName("C")] AnalysisFactorLevel C,
    [property: JsonPropertyName("E")] AnalysisFactorLevel E,
    [property: JsonPropertyName("A")] AnalysisFactorLevel A,
    [property: JsonPropertyName("stability")] AnalysisFactorLevel Stability,
    [property: JsonPropertyName("maturityIndex")] double? MaturityIndex,
    [property: JsonPropertyName("maturityLevel")] string? MaturityLevel);

internal sealed record AnalysisInterests(
    [property: JsonPropertyName("hollandCode")] string HollandCode,
    [property: JsonPropertyName("types")] IReadOnlyDictionary<string, double> Types,
    [property: JsonPropertyName("differentiation")] double Differentiation,
    [property: JsonPropertyName("consistency")] string Consistency,
    [property: JsonPropertyName("mappedFields")] IReadOnlyList<string> MappedFields);

internal sealed record AnalysisActivity(
    [property: JsonPropertyName("MOT")] double Mot,
    [property: JsonPropertyName("SELF")] double Self,
    [property: JsonPropertyName("SOCA")] double Soca,
    [property: JsonPropertyName("ENG")] double Eng,
    [property: JsonPropertyName("activityIndex")] double? ActivityIndex,
    [property: JsonPropertyName("activityLevel")] string? ActivityLevel,
    [property: JsonPropertyName("needsAttention")] bool NeedsAttention);

internal sealed record AnalysisCustomTestScale(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("pct")] double Pct,
    [property: JsonPropertyName("level")] string Level);

/// <summary>
/// Superadmin `Custom` (`SUM` strategiyali) anketalar natijasi — `docs/09` §3: "AI ularni umumiy
/// portretga bog'laydi, lekin tip va kasb xulosalari faqat ilmiy metodikalarga tayanadi".
/// </summary>
internal sealed record AnalysisCustomTest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("scales")] IReadOnlyList<AnalysisCustomTestScale> Scales);
