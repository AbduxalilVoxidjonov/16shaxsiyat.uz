namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Tizim metodikalarining shkala nomlari — `(ScoringStrategyCode, ScaleCode) → (NameUz, DescriptionUz?)`.
///
/// <para>
/// <b>Nima uchun umuman kerak.</b> Tizim metodikalarida (`IsSystem = true`) shkala nomi HECH
/// QAYERDA saqlanmaydi: seed JSON'da har savolda faqat `"scale": "R"` bor, `TestScale` yozuvlari
/// esa ATAYLAB faqat `Custom` anketalarda to'ldiriladi (`docs/07` §3.4: "Shkalalar — faqat
/// Custom"). Natijada admin katalogida `ART`, `SOCA`, `CONV` kabi xom kodlar ko'rinadi va
/// ularning ma'nosini sahifadan bilib bo'lmaydi. Nomlar `docs/03` da bor, lekin faqat hujjatda.
/// </para>
///
/// <para>
/// <b>Nima uchun bu yerda, domenda.</b> Muqobil — nomni frontend i18n jadvaliga yozish edi.
/// U holda bitta ustun IKKI manbadan to'lardi: `Custom` uchun bazadagi `TestScale.NameUz`,
/// tizim uchun locale fayli. Ikki manba vaqt o'tib bir-biridan uziladi va farq JIMGINA yuzaga
/// chiqadi (seed'ga yangi shkala qo'shilsa, frontend eski jadvalni ko'rsatishda davom etadi).
/// Shu sabab nomni HAR DOIM backend beradi (`CatalogQuestionItemDto.ScaleNameUz`), frontend esa
/// faqat ko'rsatadi.
/// </para>
///
/// <para>
/// <b>Nima uchun kalit `ScoringStrategyCode`, anketa kodi EMAS.</b> Shkala kodlari — scoring
/// STRATEGIYASINING shartnomasi: `RiasecStrategy` aynan `R`/`I`/`ART`/`SOC`/`ENT`/`CONV` bo'yicha
/// hisoblaydi, `Mbti16Strategy` esa `EI`/`SN`/`TF`/`JP` bo'yicha. Anketa kodi esa o'zgarishi,
/// nusxalanishi yoki `Custom` anketada boshqacha bo'lishi mumkin. Bu <see cref="PersonalityBattery.RoleOf(TestKind, TestScoringMode, string?)"/>
/// da 2026-09-03 da qabul qilingan qaror bilan ham bir xil: rol ham strategiya kodi bo'yicha
/// aniqlanadi, anketa kodi bo'yicha emas (`docs/06` qarorlar jurnali). Seed tizim metodikasini
/// `ScoringStrategyCode = dto.Code` bilan quradi (`SeedDataLoader.ToDomainSystemTestDefinition`),
/// shu sabab bugungi to'rt metodikada ikkalasi ustma-ust tushadi — lekin bog'lanish
/// strategiyaga, ya'ni formulaga.
/// </para>
///
/// <para>
/// <b>Manba:</b> nomlar `docs/03-psixologik-metodikalar.md` dan SO'ZMA-SO'Z ko'chirilgan —
/// §2.1 (`MBTI16`), §3.1 (`BIG5`), §4.1 (`RIASEC`), §5.1 (`ACTIVITY`). Boshqa manbadan
/// (masalan 16Personalities/NERIS tip nomlari) olinmaydi — `CLAUDE.md` 6a-qoidasi;
/// `docs/03` allaqachon shu qoidaga moslab yozilgan.
/// </para>
/// </summary>
public static class SystemScaleCatalog
{
    /// <summary>Bitta shkalaning katalogdagi yozuvi — kod, o'zbekcha nomi va (bo'lsa) qisqa tavsifi.</summary>
    /// <param name="Code">Shkala kodi — savoldagi <see cref="Question.Scale"/> bilan bir xil.</param>
    /// <param name="NameUz">O'zbekcha nomi (`docs/03` dan so'zma-so'z).</param>
    /// <param name="DescriptionUz">Qisqa izoh — `docs/03` da bo'lsa; aks holda <c>null</c>.</param>
    public sealed record SystemScale(string Code, string NameUz, string? DescriptionUz);

    /// <summary>`docs/03` §2.1 — 4 dixotomiya o'qi.</summary>
    private static readonly SystemScale[] Mbti16Scales =
    [
        new("EI", "Ekstraversiya/Introversiya", null),
        new("SN", "Sezish/Intuitsiya", null),
        new("TF", "Fikrlash/His-tuyg'u", null),
        new("JP", "Tartib/Moslashuvchanlik", null),
    ];

    /// <summary>`docs/03` §3.1 — 5 omil (OCEAN). `N` uchun tavsif §3.2 eslatmasidan.</summary>
    private static readonly SystemScale[] BigFiveScales =
    [
        new("O", "Ochiqlik", null),
        new("C", "Vijdonlilik", null),
        new("E", "Ekstraversiya", null),
        new("A", "Kelishuvchanlik", null),
        new("N", "Emotsional beqarorlik / neyrotizm", "Hisobotda \"emotsional barqarorlik\" sifatida teskari ko'rsatiladi: StabilityPct = 100 − N_pct."),
    ];

    /// <summary>
    /// `docs/03` §4.1 — 6 tip. Kodlar `A`/`S`/`E`/`C` emas, `ART`/`SOC`/`ENT`/`CONV`: bir harfli
    /// variant Big Five omillari bilan to'qnashardi (o'sha bo'limdagi eslatma).
    /// </summary>
    private static readonly SystemScale[] RiasecScales =
    [
        new("R", "Realistik", "amaliy/texnik"),
        new("I", "Tadqiqotchi", null),
        new("ART", "Artistik", null),
        new("SOC", "Ijtimoiy", null),
        new("ENT", "Tadbirkor", null),
        new("CONV", "Konvensional", "tartibli/hujjat"),
    ];

    /// <summary>`docs/03` §5.1 jadvali — nom va "nima o'lchaydi" ustuni tavsif sifatida.</summary>
    private static readonly SystemScale[] ActivityScales =
    [
        new("MOT", "Ta'lim motivatsiyasi", "O'qishga ichki qiziqish, maqsad aniqligi"),
        new("SELF", "O'z-o'zini boshqarish", "Vaqtni rejalash, intizom, e'tiborni ushlab turish"),
        new("SOCA", "Ijtimoiy faollik", "Jamoa ishi, tashabbus, to'garak/tadbirlar"),
        new("ENG", "Band bo'lish darajasi", "Darsdan tashqari mashg'ulot, kitob, sport, hobbi"),
    ];

    /// <summary>
    /// Kalit — <see cref="TestDefinition.ScoringStrategyCode"/> / <c>IScoringStrategy.StrategyCode</c>
    /// (`docs/03` §8). `SUM` bu yerda YO'Q va bo'lmaydi ham: `SUM` — superadmin anketalari uchun,
    /// ularda shkala nomi bazada (`TestScale.NameUz`) turadi.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<SystemScale>> ScalesByStrategy =
        new Dictionary<string, IReadOnlyList<SystemScale>>(StringComparer.Ordinal)
        {
            ["MBTI16"] = Mbti16Scales,
            ["BIG5"] = BigFiveScales,
            ["RIASEC"] = RiasecScales,
            ["ACTIVITY"] = ActivityScales,
        };

    /// <summary>Katalog qamrab olgan strategiya kodlari (drift qo'riqchisi test uchun ham).</summary>
    public static IReadOnlyList<string> StrategyCodes { get; } = [.. ScalesByStrategy.Keys];

    /// <summary>
    /// Berilgan strategiyaning barcha shkalalari `docs/03` dagi tartibda. Tanilmagan strategiya
    /// (`SUM`, `null`, superadmin kodi) → bo'sh ro'yxat, istisno EMAS: "bilmayman" holati
    /// degradatsiya bo'lib, xato emas.
    /// </summary>
    public static IReadOnlyList<SystemScale> ForStrategy(string? scoringStrategyCode)
    {
        if (string.IsNullOrWhiteSpace(scoringStrategyCode))
        {
            return [];
        }

        return ScalesByStrategy.TryGetValue(scoringStrategyCode, out var scales) ? scales : [];
    }

    /// <summary>
    /// Shkala nomini topadi. Strategiya yoki kod tanilmasa <c>null</c> — chaqiruvchi shunda
    /// faqat xom kodni ko'rsatadi. Taxminiy nom BERILMAYDI: noto'g'ri nom kodning o'zidan
    /// yomonroq, chunki u ishonchli ko'rinadi.
    /// </summary>
    public static string? FindNameUz(string? scoringStrategyCode, string? scaleCode) =>
        Find(scoringStrategyCode, scaleCode)?.NameUz;

    /// <summary>Shkalaning to'liq yozuvi (nom + tavsif) yoki <c>null</c>.</summary>
    public static SystemScale? Find(string? scoringStrategyCode, string? scaleCode)
    {
        if (string.IsNullOrWhiteSpace(scaleCode))
        {
            return null;
        }

        foreach (var scale in ForStrategy(scoringStrategyCode))
        {
            if (string.Equals(scale.Code, scaleCode, StringComparison.Ordinal))
            {
                return scale;
            }
        }

        return null;
    }
}
