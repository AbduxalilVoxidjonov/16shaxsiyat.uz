using System.Text.Json.Serialization;

namespace StudentRoadMap.Application.Admin.Students;

/// <summary>
/// `GET /api/admin/students` ro'yxat elementi — `docs/07-api-shartnoma.md` 3.2-bo'lim:
/// `id, fullName, schoolName, grade, classLetter, phone, lastAssessmentStatus, personalityType,
/// maturityIndex, activityLevel, needsAttention, reliabilityFlag, lastAssessmentAt`.
///
/// `LastAssessmentStatus`/`ReliabilityFlag` — `Student` SNAPSHOT ustunlarida YO'Q (`docs/05`
/// 2-bo'lim `students` DDL'ida bunday ustun yo'q, faqat `docs/07`ning misol javobida bor).
/// `ListStudentsQueryHandler` bularni SAHIFA hajmida (≤100 o'quvchi) alohida, qimmat bo'lmagan
/// batch so'rov bilan `assessments`dan to'ldiradi — PM'ga savol: kelajakda bular ham
/// `students` snapshot ustunlariga ko'chirilsinmi (`docs/04`/`docs/05`ni yangilab)?
///
/// <para>
/// **`PersonalityTypeName`** (2026-09-03, egasining talabi: "qisqartirib yozilgan 16 ta
/// shaxsiyatni to'liq nomi bilan chiqar"): `TypeCatalog.NameUz` — `PersonalityType` KODIGA
/// (`INTJ`) mos to'liq nom. Nomlar `type-catalog.json` dan keladi (mustaqil yozilgan,
/// `CLAUDE.md` 6a-bandi). Katalogda yozuv topilmasa `null` — UI shunda faqat kodni
/// ko'rsatadi, soxta nom O'YLAB TOPILMAYDI. Sahifa hajmida bitta batch so'rov
/// (`ExportStudentsQueryHandler` dagi `typeNameCache` bilan bir xil naqsh).
/// </para>
/// <para>
/// `source` ustuni YO'Q (2026-09-07): ro'yxat FAQAT maktab o'quvchilarini qaytaradi — ommaviy
/// makon foydalanuvchilari `GET /api/admin/public-space/users` da, shu sabab qatorni manba
/// bo'yicha belgilash ma'nosiz (`AdminStudentFilterBuilder` izohi).
/// </para>
/// </summary>
public sealed record AdminStudentListItemDto(
    Guid Id,
    string FullName,
    string SchoolName,
    int Grade,
    string? ClassLetter,
    /// <summary>P52: anonim o'quvchida (`IsAnonymous`) `null` — UI "—" ko'rsatadi.</summary>
    string? Phone,
    string? LastAssessmentStatus,
    string? PersonalityType,
    string? PersonalityTypeName,
    double? MaturityIndex,
    string? ActivityLevel,
    bool NeedsAttention,
    string? ReliabilityFlag,
    DateTimeOffset? LastAssessmentAt);

/// <summary>`GET /api/admin/students/{id}` — `docs/07` 3.2-bo'lim `student` qismi.</summary>
public sealed record AdminStudentDetailDto(
    Guid Id,
    string FullName,
    /// <summary>P52: anonim o'quvchida (`IsAnonymous`) `null`.</summary>
    DateOnly? BirthDate,
    /// <summary>P52: anonim o'quvchida (`IsAnonymous`) `null` (yosh hisoblab bo'lmaydi).</summary>
    int? Age,
    string Gender,
    int Grade,
    string? ClassLetter,
    /// <summary>P52: anonim o'quvchida (`IsAnonymous`) `null`.</summary>
    string? Phone,
    string? ParentPhone,
    string? Email,
    AdminStudentSchoolRefDto School,
    DateTimeOffset ConsentGivenAt,
    DateTimeOffset CreatedAt);

public sealed record AdminStudentSchoolRefDto(Guid Id, string Name);

/// <summary>`docs/07` 3.2-bo'lim `assessments[]` — sessiyalar ro'yxati (eng yangisi birinchi).</summary>
public sealed record AdminAssessmentSummaryDto(
    Guid Id,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? DurationMinutes,
    double? ReliabilityScore,
    string? ReliabilityFlag,
    bool IsLatest);

/// <summary>`docs/07` 3.2-bo'lim to'liq javobi: `student`, `assessments`, `latestAssessment`.</summary>
public sealed record AdminStudentProfileDto(
    AdminStudentDetailDto Student,
    IReadOnlyList<AdminAssessmentSummaryDto> Assessments,
    AdminLatestAssessmentDto? LatestAssessment);

/// <summary>
/// `docs/07` 3.2-bo'lim `latestAssessment` — `results`/`aiAnalysis`/`aiHistory`/`tests`/
/// `hasPersonalityBattery`.
///
/// <para>
/// ⚠️ **Egasi topgan kamchilik (2026-09-12) tuzatildi.** `Results`dagi `null` blok ikki
/// BUTUNLAY boshqa holatni ajrata olmasdi: (1) test bu sessiyada BOR, lekin hali hisoblanmagan;
/// (2) test bu dasturda umuman YO'Q. Admin profili ikkalasini ham "Hali natija yo'q" deb
/// bir xil ko'rsatardi — so'rovnoma-only sessiyada (shaxsiyat batareyasisiz) 16 tip
/// diagrammalari va "hisoblanmagan" degan taassurot bergan xato ekran chiqardi.
/// <see cref="Tests"/> — shu sessiyaga biriktirilgan HAMMA test blokining ro'yxati
/// (`DisplayOrder` bo'yicha), mijoz shundan "bu test umuman yo'q" (ro'yxatda yo'q) bilan
/// "bor-u hali hisoblanmagan" (ro'yxatda bor, natija bloki `null`) holatlarini ajratadi.
/// <see cref="HasPersonalityBattery"/> — <see cref="StudentRoadMap.Domain.Catalog.PersonalityBattery"/>
/// domen qoidasidan (`Kind == Standard &amp;&amp; ScoringMode == Scored`), kod ro'yxatidan EMAS —
/// `AdminProgramDetailDto.HasPersonalityBattery`/`GetSessionStateResult.HasPersonalityBattery`
/// bilan BIR XIL manba.
/// </para>
/// </summary>
public sealed record AdminLatestAssessmentDto(
    Guid Id,
    AdminTestResultsDto Results,
    AdminAiAnalysisDto? AiAnalysis,
    IReadOnlyList<AdminAiHistoryItemDto> AiHistory,
    IReadOnlyList<AdminLatestAssessmentTestItemDto> Tests,
    bool HasPersonalityBattery);

/// <summary>
/// `docs/07` 3.2-bo'lim `latestAssessment.tests[]` elementi — sessiyaga biriktirilgan bitta
/// test bloki. `Code`/`ScoringMode` — `TestDefinition`dan, `Status` — `AssessmentTest.Status`dan
/// (`docs/07` 3.3-bo'limdagi `AdminAssessmentTestItemDto` bilan bir xil atama, lekin bu yerda
/// faqat "bor/yo'q va holati" kerak, savol soni emas — shu sabab qisqaroq shakl).
/// `scale`/`scaleDirection` bu yerda ham YO'Q (`CLAUDE.md` 9-band).
///
/// <para>
/// **`BatteryRole`** (code-review, 2026-09-14): frontend hozir batareya kartasi mavjudligini
/// `Code == "MBTI16"|"BIG5"|"RIASEC"|"ACTIVITY"` satri bo'yicha aniqlaydi, backend esa
/// `results.MBTI16/...` bloklarini `PersonalityBattery.RoleOf` DOMEN qoidasidan (`Kind`,
/// `ScoringMode`, `ScoringStrategyCode`) to'ldiradi. Kod versiyalansa (masalan `MBTI16-V2`)
/// backend natijani beradi, frontend esa satr mos kelmagani uchun kartani yashiradi — ikki
/// tomon jimgina chalkashadi. Shu sabab rol backend'da hisoblanib DTO orqali eksport qilinadi:
/// `"None" | "PersonalityType" | "Traits" | "CareerInterest" | "Activity"`
/// (<see cref="StudentRoadMap.Domain.Catalog.PersonalityBatteryRole"/>).
/// </para>
/// </summary>
public sealed record AdminLatestAssessmentTestItemDto(
    string Code,
    string NameUz,
    string Status,
    string ScoringMode,
    string BatteryRole);

/// <summary>
/// Har test — mavjud bo'lsagina to'ldiriladi (o'quvchi hali yechmagan test `null`).
///
/// <para>
/// **JSON kalitlari `TestDefinition.Code` bilan HARFMA-HARF bir xil** — `MBTI16`, `BIG5`,
/// `RIASEC`, `ACTIVITY` (`docs/07-api-shartnoma.md` 3.2-bo'lim, "Natija to'plami kalitlari").
/// Shu sabab har xususiyatda ANIQ `JsonPropertyName` bor: C# nomlari (`Mbti16`, `Big5`…)
/// standart camelCase siyosati bilan `mbti16`/`big5`/`riasec`/`activity` bo'lib ketardi va
/// mijoz `results.MBTI16` ni topa olmay bo'limlarni JIMGINA bo'sh ko'rsatardi (2026-09-03
/// tekshiruvi). Kalitlar shartnoma — ularni o'zgartirish `docs/07` bilan birga qilinadi.
/// </para>
/// </summary>
public sealed record AdminTestResultsDto(
    [property: JsonPropertyName("MBTI16")] AdminMbti16Dto? Mbti16,
    [property: JsonPropertyName("BIG5")] AdminBig5Dto? Big5,
    [property: JsonPropertyName("RIASEC")] AdminRiasecDto? Riasec,
    [property: JsonPropertyName("ACTIVITY")] AdminActivityDto? Activity);

public sealed record AdminAxisDto(double Pct, string Letter, bool Borderline);

/// <summary>`docs/07` 3.2: `{resultCode, typeName, axes, borderlineAxes}`. `scale`/`scaleDirection` YO'Q (`CLAUDE.md` 9-band).</summary>
public sealed record AdminMbti16Dto(
    string ResultCode,
    string TypeName,
    IReadOnlyDictionary<string, AdminAxisDto> Axes,
    IReadOnlyList<string> BorderlineAxes);

public sealed record AdminFactorDto(double Raw, double Pct, string Level);

/// <summary>`docs/07` 3.2: `{factors, stabilityPct, maturityIndex, maturityLevel}`.</summary>
public sealed record AdminBig5Dto(
    IReadOnlyDictionary<string, AdminFactorDto> Factors,
    double StabilityPct,
    double? MaturityIndex,
    string? MaturityLevel);

public sealed record AdminCareerFieldDto(string Name, IReadOnlyList<string> Professions);

/// <summary>
/// `docs/07` 3.2: `{resultCode, types, differentiation, consistency, careerFields}`.
///
/// <para>
/// `Types` lug'atining KALITLARI — bitta harfli Holland mnemonikasi `R, I, A, S, E, C`
/// (`docs/03` §4.1 "matnda qisqalik uchun R-I-A-S-E-C harflari", `docs/07` 3.2), bazadagi
/// `Scale` kodlari (`R, I, ART, SOC, ENT, CONV`) EMAS. `ResultCode` (Holland kodi, masalan
/// `"IRA"`) ham aynan shu harflardan iborat — mijoz kodning harflarini `types` kalitlari
/// bilan to'g'ridan-to'g'ri solishtira olishi uchun. O'girish `StudentProfileMapping`da.
/// </para>
/// </summary>
public sealed record AdminRiasecDto(
    string ResultCode,
    IReadOnlyDictionary<string, double> Types,
    double Differentiation,
    string Consistency,
    IReadOnlyList<AdminCareerFieldDto> CareerFields);

/// <summary>`docs/07` 3.2: `{scales, activityIndex, activityLevel, needsAttention}`.</summary>
public sealed record AdminActivityDto(
    IReadOnlyDictionary<string, double> Scales,
    double? ActivityIndex,
    string? ActivityLevel,
    bool NeedsAttention);

/// <summary>
/// AI hisobotining bitta kuchli tomoni — `docs/09-ai-analiz-moduli.md` 5-bo'lim sxemasidagi
/// `strengths[]` elementi (`title`, `description`, `evidence`). Zaxira (shablon) hisobotda
/// (`docs/09` 11-bo'lim) faqat `Title` bo'ladi, qolganlari `null` — bo'sh satr EMAS.
/// </summary>
public sealed record AdminAiStrengthDto(string Title, string? Description, string? Evidence);

/// <summary>`docs/09` 5-bo'lim `growthAreas[]`: `title`, `description`, `actionStep`.</summary>
public sealed record AdminAiGrowthAreaDto(string Title, string? Description, string? ActionStep);

/// <summary>
/// `docs/09` 5-bo'lim `attentionFlags[]`: `code`, `message`, `severity` (`info|attention|high`).
/// `Code` — eski/zaxira yozuvlarda (sxemadan oldin saqlangan satrli bayroqlar) `null` bo'lishi
/// mumkin. `Severity` har doim uchta qiymatdan biri: noma'lum qiymat `"attention"`ga keltiriladi.
/// </summary>
public sealed record AdminAiAttentionFlagDto(string? Code, string Message, string Severity);

/// <summary>`docs/09` 5-bo'lim `careerSuggestions[]`: `field`, `why`, `exampleProfessions?`, `nextSteps`.</summary>
public sealed record AdminCareerSuggestionDto(
    string Field,
    string Why,
    IReadOnlyList<string> ExampleProfessions,
    IReadOnlyList<string> NextSteps);

/// <summary>
/// `docs/07` 3.2 `aiAnalysis` — **shakl manbai `docs/09-ai-analiz-moduli.md` 5-bo'lim JSON
/// sxemasi** (AI aynan shunga javob beradi va post-filtr shunga tayanadi).
/// <para>
/// Mazmun `AiAnalysis.ResponseJson`dan (validatsiyadan O'TGAN javob) o'qiladi — `AiAnalysisContent`
/// ga qarang. Ilgari DTO faqat entity ustunlaridagi YASSILANGAN (satrga aylantirilgan) nusxani
/// qaytarar edi, shu sababli `learningStyle`, `motivationProfile`, `activityAssessment`,
/// `disclaimer`, `reliabilityNote` admin ekraniga UMUMAN chiqmasdi, `strengths`/`growthAreas`/
/// `attentionFlags` esa tuzilmasini (`title`/`description`/`evidence`, `severity`) yo'qotardi.
/// </para>
/// <para>
/// **`IsFallbackReport`** (`docs/09` 11-bo'lim, `AiAnalysis.CreateFallbackReport`): `true` bo'lsa
/// bu matnni AI YOZMAGAN — zanjirdagi barcha provayder yiqilgach tizim shablon hisobot yozgan.
/// Bunday yozuvda `ResponseJson` yo'q, shuning uchun faqat ustunlardan tiklanadigan bo'limlar
/// (`summary`, `personalityPortrait`, `strengths.title`, …) to'ladi, qolganlari `null`.
/// </para>
/// <para>
/// **`IsModerated`** (`docs/09` 6-bo'lim, 3-band): taqiqlangan atama IKKINCHI urinishda ham
/// topilgan va javob "moderatsiya qilindi" belgisi bilan saqlangan (`AttentionFlags`da
/// `MODERATION_REQUIRED`). Bu matn post-filtrdan TOZA holda o'tmagan — UI uni jimgina oddiy
/// tahlil sifatida ko'rsatmasligi shart (`CLAUDE.md` 6-qoida: "AI tashxis qo'ymaydi").
/// </para>
/// <para>
/// **`Disclaimer` va `ReliabilityNote`** hisobotning cheklovlarini aytadi — DTO'da bor va UI'da
/// yashirilmaydi (`docs/09` 4.2 talab 13 va "ISHONCHLILIK" bandi).
/// </para>
/// </summary>
public sealed record AdminAiAnalysisDto(
    Guid Id,
    string Status,
    string Provider,
    string Model,
    string PromptVersion,
    DateTimeOffset CreatedAt,
    bool IsFallbackReport,
    bool IsModerated,
    string? ErrorMessage,
    string? Summary,
    string? PersonalityPortrait,
    IReadOnlyList<AdminAiStrengthDto> Strengths,
    IReadOnlyList<AdminAiGrowthAreaDto> GrowthAreas,
    string? LearningStyle,
    string? MotivationProfile,
    string? ActivityAssessment,
    IReadOnlyList<AdminCareerSuggestionDto> CareerSuggestions,
    IReadOnlyList<string> StudentRecommendations,
    IReadOnlyList<string> TeacherNotes,
    IReadOnlyList<string> ParentNotes,
    IReadOnlyList<AdminAiAttentionFlagDto> AttentionFlags,
    string? ReliabilityNote,
    string? Disclaimer);

public sealed record AdminAiHistoryItemDto(Guid Id, string Provider, DateTimeOffset CreatedAt, string Status, bool IsCurrent);
