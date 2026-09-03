namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// "Shaxsiyat batareyasi" — ilmiy metodikalar to'plami (seed'dan keladigan MBTI16/BIG5/RIASEC/
/// ACTIVITY) mavjudligini aniqlaydigan YAGONA domen qoidasi (`docs/06` 8-bo'lim, 2026-09-02
/// qaror: dasturga batareya biriktirilmasligi mumkin — "admin hal qiladi biriktirishni").
///
/// <para>
/// <b>Nega kod (`"MBTI16"`) bo'yicha emas.</b> Ilgari frontend bu savolga sessiya testlari
/// ichidan `"MBTI16"` satrini qidirib javob berardi. Bu ikki holatda JIMGINA buziladi:
/// (1) `Custom` dastur boshqa kodli metodika ishlatsa; (2) kod o'zgarsa/versiyalansa. Ikkala
/// holatda ham natija ekrani noto'g'ri holatga tushadi va HECH QANDAY xato ko'rinmaydi.
/// Shu sabab mezon satrga emas, domen atributlariga bog'landi — yangi "sehrli satr" kiritilmadi.
/// </para>
///
/// <para>
/// <b>Mezon: <c>Kind == TestKind.Standard &amp;&amp; ScoringMode == TestScoringMode.Scored</c>.</b>
/// <list type="bullet">
///   <item><description>
///     <c>TestKind.Standard</c> — domenda ALLAQACHON mavjud va aynan shu ma'noni tashiydi:
///     "Ilmiy metodika (seed'dan keladi)". Seed batareyani `TestDefinition.CreateSystemPublished`
///     orqali quradi, u esa har doim `Standard` beradi — ya'ni mezon seed haqiqatiga tayanadi,
///     alohida ro'yxatga emas.
///   </description></item>
///   <item><description>
///     <c>ScoringMode.Scored</c> — `Survey` rejimidagi anketa ballanmaydi (`TestResult` yozilmaydi,
///     `CompositeScorer`/AI xulosasiga kirmaydi), demak undan hech qachon shaxsiyat tipi chiqmaydi.
///     Nazariy jihatdan `TestDefinition.Create(kind: Standard, scoringMode: Survey)` mumkin, shu
///     sabab bu shart oshiqcha emas — qoidani "ball chiqmaydigan" holatdan himoyalaydi.
///   </description></item>
/// </list>
/// </para>
///
/// <para>
/// <b>Nima uchun boshqa nomzodlar emas:</b>
/// <list type="bullet">
///   <item><description>
///     <c>IsSystem</c> — bu TAHRIRLASH QULFI (BR-8: savol/shkala o'zgartirilmaydi), ya'ni
///     ma'muriy tushuncha; anketa NIMANI o'lchashi haqida hech narsa demaydi. Bundan tashqari
///     `Duplicate()` ataylab `IsSystem = false` nusxa yaratadi.
///   </description></item>
///   <item><description>
///     <c>ScoringStrategyCode != null</c> — superadmin yaratgan HAR QANDAY ballanadigan
///     `Custom` anketada ham bo'ladi, ya'ni mezon juda keng: batareyasiz dastur ham "bor"
///     deb ko'rinardi va xato aynan qaytadan paydo bo'lardi.
///   </description></item>
///   <item><description>
///     "Dasturda test bor" — deyarli har doim rost, hech narsani ajratmaydi.
///   </description></item>
/// </list>
/// </para>
/// </summary>
public static class PersonalityBattery
{
    /// <summary>
    /// Batareya rollarini beradigan scoring strategiyalari — `TestDefinition.ScoringStrategyCode`
    /// (`docs/04` §2.7) va `IScoringStrategy.StrategyCode` (`docs/03` §8) bilan BIR XIL registr.
    /// Bu yerdagi satrlar YANGI sehrli satr emas: ular strategiya identifikatorlari (formulaning
    /// nomi), anketa kodi emas. Ular strategiya sinflaridan chetga chiqib ketmasligi
    /// `PersonalityBatteryTests.RoleOf_HarBirStrategiya...` testida qulflangan — test aynan
    /// `Mbti16Strategy`/`BigFiveStrategy`/`RiasecStrategy`/`ActivityStrategy`ning o'z
    /// `StrategyCode` qiymatini beradi.
    /// </summary>
    private const string PersonalityTypeStrategyCode = "MBTI16";

    private const string TraitsStrategyCode = "BIG5";

    private const string CareerInterestStrategyCode = "RIASEC";

    private const string ActivityStrategyCode = "ACTIVITY";

    /// <summary>Berilgan anketa atributlari ilmiy batareyaga tegishlimi.</summary>
    public static bool Includes(TestKind kind, TestScoringMode scoringMode) =>
        kind == TestKind.Standard && scoringMode == TestScoringMode.Scored;

    /// <summary>Berilgan anketa ilmiy batareyaga tegishlimi.</summary>
    public static bool Includes(TestDefinition testDefinition)
    {
        ArgumentNullException.ThrowIfNull(testDefinition);

        return Includes(testDefinition.Kind, testDefinition.ScoringMode);
    }

    /// <summary>To'plamda kamida bitta batareya anketasi bormi (dastur/sessiya darajasidagi bayroq).</summary>
    public static bool ContainedIn(IEnumerable<TestDefinition> testDefinitions)
    {
        ArgumentNullException.ThrowIfNull(testDefinitions);

        return testDefinitions.Any(Includes);
    }

    /// <summary>
    /// Anketaning batareya ICHIDAGI roli — "bu natija nimani tashiydi?" (<see cref="PersonalityBatteryRole"/>).
    /// Rol ikki shartning KESISHMASI:
    /// <list type="number">
    ///   <item><description>
    ///     anketa umuman batareyaga kirishi shart (<see cref="Includes(TestKind, TestScoringMode)"/>) —
    ///     shu bilan `hasPersonalityBattery` bayrog'i (`docs/07` 1.3) va natija ekranidagi
    ///     ma'lumot BIR XIL qoidadan kelib chiqadi: bayroq "batareya yo'q" desa, o'sha sessiyadan
    ///     shaxsiyat tipi ham, Holland kodi ham, `MaturityIndex` ham CHIQMAYDI;
    ///   </description></item>
    ///   <item><description>
    ///     ballni qaysi ALGORITM hisoblagani (`ScoringStrategyCode`) — natijaning ma'nosi shundan
    ///     kelib chiqadi, anketa kodidan emas.
    ///   </description></item>
    /// </list>
    ///
    /// <para>
    /// Tanilmagan strategiya (masalan superadmin anketasining `SUM`i) yoki batareyaga kirmaydigan
    /// anketa — <see cref="PersonalityBatteryRole.None"/>. "Bilmayman"ni "shaxsiyat tipi" bilan
    /// almashtirmaymiz: `None` chaqiruvchida "ma'lumot yo'q" (`null`) ga aylanadi, `0` yoki
    /// tasodifiy natijaga emas (`docs/06` 8-bo'lim, 2026-09-02 "batareya majburiy emas" qarori).
    /// </para>
    /// </summary>
    public static PersonalityBatteryRole RoleOf(TestKind kind, TestScoringMode scoringMode, string? scoringStrategyCode)
    {
        if (!Includes(kind, scoringMode) || string.IsNullOrWhiteSpace(scoringStrategyCode))
        {
            return PersonalityBatteryRole.None;
        }

        return scoringStrategyCode switch
        {
            PersonalityTypeStrategyCode => PersonalityBatteryRole.PersonalityType,
            TraitsStrategyCode => PersonalityBatteryRole.Traits,
            CareerInterestStrategyCode => PersonalityBatteryRole.CareerInterest,
            ActivityStrategyCode => PersonalityBatteryRole.Activity,
            _ => PersonalityBatteryRole.None,
        };
    }

    /// <summary>Berilgan anketaning batareya ichidagi roli.</summary>
    public static PersonalityBatteryRole RoleOf(TestDefinition testDefinition)
    {
        ArgumentNullException.ThrowIfNull(testDefinition);

        return RoleOf(testDefinition.Kind, testDefinition.ScoringMode, testDefinition.ScoringStrategyCode);
    }
}
