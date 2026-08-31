namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Barcha scoring koeffitsiyentlari va chegaralari — `docs/03-psixologik-metodikalar.md`.
/// Sehrli raqam yo'q: har bir formulaning har bir raqami shu yerda nomlangan.
/// </summary>
public static class ScoringConstants
{
    // --- Umumiy (docs/03 §1) ---

    /// <summary>Likert-5 javobining eng kichik qiymati.</summary>
    public const int Likert5Min = 1;

    /// <summary>Likert-5 javobining eng katta qiymati.</summary>
    public const int Likert5Max = 5;

    /// <summary>Likert-7 javobining eng kichik qiymati (faqat `SUM` strategiyasida ishlatiladi).</summary>
    public const int Likert7Min = 1;

    /// <summary>Likert-7 javobining eng katta qiymati (faqat `SUM` strategiyasida ishlatiladi).</summary>
    public const int Likert7Max = 7;

    /// <summary>Yaxlitlash aniqligi — hujjatda aytilmagan barcha foiz/indeks natijalar uchun.</summary>
    public const int RoundingDecimals = 2;

    /// <summary>Joriy scoring formula versiyasi — `docs/03` §8: "Formula o'zgarsa
    /// `TestDefinition.Version` oshiriladi... `TestResult.ScoringVersion`da qaysi versiya
    /// ishlatilgani qoladi". Har bir <see cref="ScoringResult"/> shu qiymatni tashiydi.</summary>
    public const int CurrentScoringVersion = 1;

    // --- MBTI16 (docs/03 §2.2) ---

    /// <summary>Chegaraviy zona pasti — `45 ≤ axisPct ≤ 55` bo'lsa `BorderlineAxes` ga qo'shiladi.</summary>
    public const double BorderlineZoneMin = 45.0;

    /// <summary>Chegaraviy zona yuqorisi.</summary>
    public const double BorderlineZoneMax = 55.0;

    /// <summary>`axisPct == 50` tie-break nuqtasi.</summary>
    public const double TieBreakPct = 50.0;

    // --- BIG5 (docs/03 §3.2–§3.3) ---

    /// <summary>Har omilga to'g'ri keladigan savol soni (docs/03 §3.1: "50 savol, 5 omil × 10 savol").
    /// `factorPct` formulasi (10..50 xom ball) shu soniga qat'iy bog'liq — mos kelmasa natija
    /// 0..100 tashqarisiga chiqib ketadi, shuning uchun strategiya bu sonni tekshiradi.</summary>
    public const int Big5QuestionsPerFactor = 10;

    /// <summary>`factorRaw` eng kichik mumkin bo'lgan qiymati (10 savol × min 1).</summary>
    public const double Big5FactorRawMin = 10.0;

    /// <summary>`factorRaw` diapazoni (`factorMax − factorMin` = 50 − 10).</summary>
    public const double Big5FactorRawRange = 40.0;

    /// <summary>Omil darajasi chegaralari (`docs/03` 3.2-jadval): `pct ≤ chegara`.</summary>
    public const double Big5LevelVeryLowMax = 20.0;

    public const double Big5LevelLowMax = 40.0;

    public const double Big5LevelAverageMax = 60.0;

    public const double Big5LevelHighMax = 80.0;

    public const string Big5LevelVeryLow = "Juda past";

    public const string Big5LevelLow = "Past";

    public const string Big5LevelAverage = "O'rtacha";

    public const string Big5LevelHigh = "Yuqori";

    public const string Big5LevelVeryHigh = "Juda yuqori";

    /// <summary>`MaturityIndex` og'irliklari (docs/03 §3.3) — yig'indisi 1.0.</summary>
    public const double MaturityWeightConscientiousness = 0.30;

    public const double MaturityWeightStability = 0.25;

    public const double MaturityWeightAgreeableness = 0.20;

    public const double MaturityWeightSelfRegulation = 0.15;

    public const double MaturityWeightOpenness = 0.10;

    /// <summary>`MaturityIndex` daraja chegaralari (docs/03 3.3-jadval): `index ≤ chegara`.</summary>
    public const double MaturityLevelFormingMax = 35.0;

    public const double MaturityLevelAverageMax = 55.0;

    public const double MaturityLevelGoodMax = 75.0;

    public const string MaturityLevelForming = "Shakllanish bosqichida";

    public const string MaturityLevelAverage = "O'rtacha";

    public const string MaturityLevelGood = "Yaxshi";

    public const string MaturityLevelHigh = "Yuqori";

    // --- RIASEC (docs/03 §4.2) ---

    /// <summary>Har tipga to'g'ri keladigan savol soni (docs/03 §4.1: "48 savol, 6 tip × 8 savol").
    /// `typePct` formulasi (8..40 xom ball) shu soniga qat'iy bog'liq — mos kelmasa natija
    /// 0..100 tashqarisiga chiqib ketadi, shuning uchun strategiya bu sonni tekshiradi.</summary>
    public const int RiasecQuestionsPerType = 8;

    /// <summary>`typeRaw` eng kichik mumkin bo'lgan qiymati (8 savol × min 1).</summary>
    public const double RiasecTypeRawMin = 8.0;

    /// <summary>`typeRaw` diapazoni (`typeMax − typeMin` = 40 − 8).</summary>
    public const double RiasecTypeRawRange = 32.0;

    /// <summary>Differensiatsiya bu qiymatdan past bo'lsa — "hali aniq shakllanmagan" bayrog'i.</summary>
    public const double RiasecLowDifferentiationThreshold = 20.0;

    public const string RiasecConsistencyHigh = "High";

    public const string RiasecConsistencyMedium = "Medium";

    public const string RiasecConsistencyLow = "Low";

    // --- ACTIVITY (docs/03 §5.2) ---

    /// <summary>Har shkalaga to'g'ri keladigan savol soni (docs/03 §5.1: "32 savol, 4 shkala × 8 savol").
    /// `scalePct` formulasi (8..40 xom ball) shu soniga qat'iy bog'liq — mos kelmasa natija
    /// 0..100 tashqarisiga chiqib ketadi, shuning uchun strategiya bu sonni tekshiradi.</summary>
    public const int ActivityQuestionsPerScale = 8;

    /// <summary>`scaleRaw` eng kichik mumkin bo'lgan qiymati (8 savol × min 1).</summary>
    public const double ActivityScaleRawMin = 8.0;

    /// <summary>`scaleRaw` diapazoni (`scaleMax − scaleMin` = 40 − 8).</summary>
    public const double ActivityScaleRawRange = 32.0;

    /// <summary>`ActivityIndex` og'irliklari (docs/03 §5.2) — yig'indisi 1.0.</summary>
    public const double ActivityWeightMotivation = 0.30;

    public const double ActivityWeightSelfRegulation = 0.30;

    public const double ActivityWeightSocialActivity = 0.20;

    public const double ActivityWeightEngagement = 0.20;

    /// <summary>`ActivityIndex` daraja chegaralari (docs/03 5.2-jadval): `index ≤ chegara`.</summary>
    public const double ActivityLevelPassiveMax = 30.0;

    public const double ActivityLevelLowActiveMax = 50.0;

    public const double ActivityLevelModerateMax = 70.0;

    public const double ActivityLevelActiveMax = 85.0;

    public const string ActivityLevelPassiveCode = "Passive";

    public const string ActivityLevelLowActiveCode = "LowActive";

    public const string ActivityLevelModerateCode = "Moderate";

    public const string ActivityLevelActiveCode = "Active";

    public const string ActivityLevelHighlyActiveCode = "HighlyActive";

    /// <summary>`ActivityIndex` shu qiymatdan past bo'lsa `NeedsAttention = true`.</summary>
    public const double ActivityNeedsAttentionThreshold = 31.0;

    // --- SUM (docs/03 §6.2) ---

    /// <summary>Har shkalada nashr qilish uchun talab qilinadigan eng kam savol soni (docs/03 §6.3).</summary>
    public const int SumMinQuestionsPerScale = 4;

    // --- Ishonchlilik indeksi (docs/03 §7) ---

    /// <summary>Javob shundan tezroq berilsa "juda tez" hisoblanadi (ms).</summary>
    public const int FastAnswerDurationThresholdMs = 900;

    /// <summary>Tez javoblar jarimasining `p × 100` ga ko'paytiruvchisi.</summary>
    public const double FastAnswerPenaltyMultiplier = 0.8;

    /// <summary>Tez javoblar jarimasining eng ko'p qiymati.</summary>
    public const double FastAnswerPenaltyCap = 40.0;

    /// <summary>Straight-lining uchun eng kam ketma-ket bir xil javoblar soni. PM qarori:
    /// har TO'LIQ shu uzunlikdagi seriya alohida blok hisoblanadi (masalan 36 ta ketma-ket
    /// bir xil javob = 3 blok, 1 emas) — `ReliabilityCalculator.CalculateStraightLiningPenalty`.</summary>
    public const int StraightLiningMinRunLength = 12;

    /// <summary>Har bir straight-lining bloki uchun jarima.</summary>
    public const double StraightLiningPenaltyPerBlock = 10.0;

    /// <summary>Straight-lining jarimasining eng ko'p qiymati.</summary>
    public const double StraightLiningPenaltyCap = 30.0;

    /// <summary>Teskari savollar ziddiyati jarimasining ko'paytiruvchisi (`d × 25`).</summary>
    public const double ReverseConflictPenaltyMultiplier = 25.0;

    /// <summary>Barcha javob bir xil bo'lsa qo'yiladigan qattiq jarima.</summary>
    public const double AllSameAnswerPenalty = 50.0;

    /// <summary>Sessiya shu daqiqadan qisqa bo'lsa jarima qo'llanadi.</summary>
    public const double ShortSessionThresholdMinutes = 6.0;

    /// <summary>Qisqa sessiya jarimasi.</summary>
    public const double ShortSessionPenalty = 20.0;

    /// <summary>`ReliabilityScore` bayroq chegaralari (docs/03 7-jadval): `score ≥ chegara`.</summary>
    public const double ReliabilityReliableMin = 70.0;

    public const double ReliabilityQuestionableMin = 40.0;
}
