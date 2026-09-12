namespace StudentRoadMap.Application.Admin.Assessments;

/// <summary>
/// `GET /api/admin/assessments` ro'yxat elementi — `docs/07-api-shartnoma.md` 3.3-bo'lim
/// jadvalida aniq JSON namunasi YO'Q (faqat yo'l/filtr jadvali). Shakl `AdminStudentListItemDto`
/// (3.2-bo'lim, mavjud namuna) uslubida tanlandi: sessiyani ro'yxatda tanib olish uchun kerakli
/// minimal maydonlar + o'quvchi/maktab nomi (JOIN'siz emas — sahifa hajmida batch, pastga qarang).
/// Shakl `docs/07` 3.3-bo'limiga rasman kiritildi (2026-09-03).
///
/// <para>
/// **`ProgramId`/`ProgramName`** (2026-09-03): `Assessment.ProgramId` domenda MAJBURIY
/// (`docs/06` 8-bo'lim, 2026-09-02 qaror), lekin ro'yxat javobiga chiqarilmagan edi — admin
/// jadvalidagi "Dastur" ustuni doim "—" ko'rsatardi. `ProgramName` — `AssessmentProgram.NameUz`;
/// dastur yozuvi topilmasa `null` (bo'sh satr yoki soxta nom EMAS).
/// </para>
/// <para>
/// **`Source` ustuni YO'Q** (2026-09-07): ro'yxat faqat maktab sessiyalarini qaytaradi
/// (`ListAssessmentsQuery` izohi), shu sabab 2026-09-06 da qo'shilgan `"School"`/`"Public"`
/// ustuni ma'nosiz bo'lib qoldi va olib tashlandi — `AdminStudentListItemDto` bilan bir xil.
/// </para>
/// </summary>
public sealed record AdminAssessmentListItemDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid SchoolId,
    string SchoolName,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? DurationMinutes,
    double? ReliabilityScore,
    string? ReliabilityFlag,
    Guid ProgramId,
    string? ProgramName);

/// <summary>
/// `GET /api/admin/assessments/{id}` javobi — `docs/07` 3.3-bo'lim.
///
/// <para>
/// **Birinchi to'rt maydon** (`id`, `results`, `aiAnalysis`, `aiHistory`) — `AdminLatestAssessmentDto`
/// (3.2-bo'lim `latestAssessment`) bilan HARFMA-HARF bir xil, shu jumladan `results` kalitlari
/// (`MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`, `AdminTestResultsDto`dagi `JsonPropertyName` bilan
/// qulflangan, 2026-09-03 qarori). Ular SHARTNOMA — o'zgartirilmaydi.
/// </para>
/// <para>
/// **Qolgan maydonlar — sessiya "sarlavhasi"** (2026-09-03): avval detal javobida sessiyaning
/// o'zi haqida HECH NARSA yo'q edi (holat, vaqtlar, ishonchlilik, o'quvchi, maktab, dastur), shu
/// sabab admin sahifasi ularni ro'yxatdan navigatsiya holati (`Link state`) orqali olardi —
/// havolani NUSXALAB ochilganda sahifa yarim bo'sh qolardi. Endi detalning o'zi to'liq.
/// </para>
/// <para>
/// **Ma'lumot yo'q ≠ nol** (`docs/06` qarorlar jurnali, 2026-09-02): yakunlanmagan sessiyada
/// `CompletedAt`/`DurationMinutes` — `null`; ishonchlilik hisoblanmagan bo'lsa
/// `ReliabilityScore`/`ReliabilityFlag` — `null`. `Student`/`School`/`Program` bog'liq yozuv
/// topilmasa (masalan o'quvchi soft-delete qilingan) `null` — bo'sh satrli soxta obyekt EMAS.
/// </para>
/// </summary>
public sealed record AdminAssessmentDetailDto(
    Guid Id,
    StudentRoadMap.Application.Admin.Students.AdminTestResultsDto Results,
    StudentRoadMap.Application.Admin.Students.AdminAiAnalysisDto? AiAnalysis,
    IReadOnlyList<StudentRoadMap.Application.Admin.Students.AdminAiHistoryItemDto> AiHistory,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? DurationMinutes,
    double? ReliabilityScore,
    string? ReliabilityFlag,
    AdminAssessmentStudentRefDto? Student,
    AdminAssessmentSchoolRefDto? School,
    AdminAssessmentProgramRefDto? Program,
    IReadOnlyList<AdminAssessmentTestItemDto> Tests);

/// <summary>Sessiya egasi — `{id, fullName}` (`docs/07` 3.3).</summary>
public sealed record AdminAssessmentStudentRefDto(Guid Id, string FullName);

/// <summary>Sessiya maktabi — `{id, name}` (`docs/07` 3.3).</summary>
public sealed record AdminAssessmentSchoolRefDto(Guid Id, string Name);

/// <summary>Sessiya dasturi — `{id, nameUz}` (`docs/07` 3.3, `AssessmentProgram`).</summary>
public sealed record AdminAssessmentProgramRefDto(Guid Id, string NameUz);

/// <summary>
/// Sessiyadagi bitta test bloki — `AssessmentTest` + uning `TestDefinition` metama'lumoti
/// (`docs/07` 3.3, 2026-09-03). Bungacha detal javobi faqat 4 ta TIZIM blokini (`results`)
/// bilardi va dasturdagi `Custom`/`Survey` anketalar admin ekranida umuman ko'rinmasdi.
///
/// <para>
/// Maydon nomi `TestCode` (`Code` emas) — `AdminAssessmentAnswerDto.TestCode` va
/// `GET /answers?testCode=` bilan bir xil atama, mijoz tomonidagi tayyor tip ham shuni
/// kutadi (`frontend/src/features/assessments/model/types.ts` → `AssessmentTestItemDto`).
/// </para>
/// <para>
/// `ScoringMode` — `Scored` | `Survey` (`docs/06` 8-bo'lim, 2026-09-02): `Survey` anketa
/// BALLANMAYDI, shu sabab uning natijasi `results`da yo'q va UI uni "0 ball" emas,
/// "Ballanmaydi (so'rovnoma)" deb ko'rsatadi. `QuestionCount`/`AnsweredCount` —
/// `AssessmentTest.TotalCount`/`AnsweredCount`, ya'ni SESSIYA snapshoti (katalogdagi joriy
/// savol soni emas): keyin anketaga savol qo'shilsa ham eski sessiya progressi buzilmaydi.
/// `scale`/`scaleDirection` bu yerda ham YO'Q (`CLAUDE.md` 9-band).
/// </para>
/// </summary>
public sealed record AdminAssessmentTestItemDto(
    Guid TestDefinitionId,
    string TestCode,
    string NameUz,
    string ScoringMode,
    string Status,
    int QuestionCount,
    int AnsweredCount);

/// <summary>
/// `GET /api/admin/assessments/{id}/answers` javobi — `docs/07` 3.3-bo'lim: "Xom javoblar
/// (audit uchun)". Bog'langan hajm (max ~190 savol/sessiya, `docs/05` §5) — sahifalanmaydi.
///
/// <para>
/// <b>Nega massiv emas, konvert</b> (2026-09-03, egasining talabi): jadvalning o'zi savolma-savol
/// ma'noni beradi, lekin ISHONCHLILIK BALLI sessiya darajasida hisoblanadi (`docs/03` §7) —
/// "nega 62" degan savolga javob berish uchun sessiya signallari (`AllSameAnswer`,
/// `ShortSession`), shkala darajasidagi teskari savol ziddiyati va `ScoringConstants`
/// CHEGARALARI ham kerak. Chegaralar javobda uzatiladi (`Thresholds`) — frontend ularni
/// QO'LDA TAKRORLAMAYDI, aks holda `ScoringConstants` o'zgarganda UI jimgina eskirib qolardi.
/// </para>
/// <para>
/// `Session`/`Scales` signallari HAR DOIM BUTUN sessiya bo'yicha hisoblanadi — `testCode`
/// filtri faqat `Answers` ro'yxatini toraytiradi. Sabab: `ReliabilityCalculator` ham sessiya
/// darajasida ishlaydi (straight-lining 4 blok bo'ylab uzluksiz sanaladi), bitta test bloki
/// ichida qayta hisoblansa BOSHQA (yolg'on) qiymat chiqardi.
/// </para>
/// </summary>
public sealed record AdminAssessmentAnswersDto(
    IReadOnlyList<AdminAssessmentAnswerDto> Answers,
    AdminAnswerSessionSignalsDto Session,
    IReadOnlyList<AdminAnswerScaleSignalDto> Scales,
    AdminAnswerThresholdsDto Thresholds);

/// <summary>
/// Bitta javob qatori — `prompts/15` MAXSUS DIQQAT #5 dagi maydonlar + javobning MA'NOSI
/// (2026-09-03).
///
/// <para>
/// ⚠️ <b>`Scale`/`ScaleDirection`/`ScaleNameUz`/`EffectiveValue` FAQAT ADMIN javobida.</b>
/// `CLAUDE.md` 9-band ommaviy (o'quvchi) API'ni nazarda tutadi: u yerda bu maydonlar
/// o'lchanayotgan konstruktni va savolning teskari ekanini ochib berardi, ya'ni o'quvchi
/// javobini moslashtirib natijani buzishi mumkin edi. Admin katalogida (`docs/07` §3.4) ular
/// allaqachon ochiq; audit jadvalida ham shu qoida amal qiladi. Sizib chiqmasligi
/// `PublicTestQuestionsEndpointTests` da xom JSON va swagger sxemasi ustidan qulflangan.
/// </para>
/// <para>
/// <b>`EffectiveValue` — jadvaldagi eng muhim ustun.</b> Teskari savolga (`ScaleDirection = -1`)
/// berilgan `5` shkalaga `1` bo'lib tushadi. Qiymat <see cref="StudentRoadMap.Domain.Scoring.ScoringMath.ApplyDirection"/>
/// — strategiyalar ishlatadigan AYNAN O'SHA domen funksiyasi — orqali hisoblanadi; bu yerda
/// formula QAYTA YOZILMAGAN.
/// </para>
/// <para>
/// `IsFastAnswer` — `DurationMs &lt; ScoringConstants.FastAnswerDurationThresholdMs`.
/// `StraightLiningBlockIndex` — javob TO'LIQ 12talik bir xil qiymat blokiga tushsa uning
/// tartib raqami (1 dan), aks holda `null`. Barcha javob bir xil bo'lsa bloklar
/// BELGILANMAYDI: `docs/03` §7.1 band 1 bo'yicha bu holatda faqat `AllSameAnswer` jarimasi
/// qo'llanadi, straight-lining esa qo'llanmaydi — UI ham shu bilan izchil bo'lishi kerak.
/// </para>
/// <para>
/// P52 (`docs/18` §2.1/§2.7): `RawValue` endi `int?` — `ShortText`/`LongText`/`Phone`
/// javoblari `TextValue`da, `MultiChoice` javoblari `SelectedValues`da (bo'sh bo'lsa
/// `null` — tanlov yo'q). Bitta savolda uchtadan FAQAT bittasi to'ldirilgan bo'ladi
/// (`Answer` shakl invarianti).
/// </para>
/// <para>
/// ⚠️ **Egasi topgan kamchilik (2026-09-12) tuzatildi.** Ilgari `MultiChoice` javobida
/// faqat xom `SelectedValues` (`[1, 3]`) ko'rinardi — admin qaysi variant tanlanganini
/// BILOLMASDI. <see cref="SelectedOptionTexts"/> — har bir tanlangan qiymatning
/// <c>AnswerOption.TextUz</c> matni, `SelectedValues` bilan BIR XIL tartibda. Variant
/// (masalan savol keyinchalik tahrirlanib, variant o'chirilgan bo'lsa) topilmasa —
/// YIQILMAYDI, o'rniga "Noma'lum variant (qiymat: N)" zaxira matni qo'yiladi (bu real
/// holat: eski javob variant o'chirilgandan keyin ham qoladi).
/// </para>
/// <para>
/// ⚠️ **`EffectiveValue`/`IsFastAnswer` — `Scored` qatorlarda `null` EMAS, `Survey`
/// qatorlarda `null`.** Ilgari `Survey` (matn/ko'p tanlov) javoblarida `EffectiveValue`
/// shunchaki `0` edi — bu "javobning qiymati 0" deb NOTO'G'RI o'qilishi mumkin edi
/// (egasi topgan kamchilik, 2026-09-12). Endi ikkalasi ham Likert semantikasiga oid — ular
/// FAQAT `ScoringMode = "Scored"` qatorlarda ma'noli, `Survey` qatorlarda `null`
/// ("qo'llanilmaydi", nol EMAS). `ScaleDirection`/`Weight` (matn/ko'p tanlov javoblari
/// uchun ma'nosiz standart qiymat, `Scale = "SURVEY"`) o'zgarishsiz qoladi — ularni
/// aniqlash uchun mijoz <see cref="ScoringMode"/> maydoniga qaraydi.
/// </para>
/// <para>
/// <see cref="ScoringMode"/> — `"Scored"` | `"Survey"` (`AdminAssessmentTestItemDto.ScoringMode`
/// bilan bir xil satr, egasi topgan kamchilik, 2026-09-12): javob QAYSI test blokidan
/// ekanini bilish uchun mijoz `tests[]` ro'yxati bilan ustma-ust solishtirmasin — belgi
/// javobning O'ZIDA bo'lsin, shunda `scale`/`scaleDirection`/`weight` ustunlarini
/// yashirish oddiy bo'ladi.
/// </para>
/// </summary>
public sealed record AdminAssessmentAnswerDto(
    Guid QuestionId,
    string QuestionCode,
    string TestCode,
    string QuestionText,
    int? RawValue,
    string? SelectedOptionText,
    IReadOnlyList<string>? SelectedOptionTexts,
    int DurationMs,
    int RevisionCount,
    DateTimeOffset AnsweredAt,
    string QuestionType,
    string ScoringMode,
    string Scale,
    string? ScaleNameUz,
    int ScaleDirection,
    decimal Weight,
    int? EffectiveValue,
    bool? IsFastAnswer,
    int? StraightLiningBlockIndex,
    string? TextValue,
    IReadOnlyList<int>? SelectedValues);

/// <summary>
/// Sessiya darajasidagi ishonchlilik signallari (`docs/03` §7) — bo'lim boshida ko'rsatiladi.
/// `Survey` (ballanmaydigan) test bloklari hisobga OLINMAYDI: `RecalculateAssessmentScoresCommandHandler`
/// ham ularni `ReliabilityCalculator`ga bermaydi, ikki joy bir xil to'plamda ishlashi shart.
/// </summary>
/// <param name="AnsweredCount">Hisobga olingan (ballanadigan) javoblar soni — `p` ning maxraji.</param>
/// <param name="FastAnswerCount">`DurationMs &lt; 900` bo'lgan javoblar soni.</param>
/// <param name="StraightLiningBlockCount">To'liq 12talik bir xil qiymat bloklari soni.</param>
/// <param name="AllSameAnswer">Barcha javob bir xil — `docs/03` §7 dagi 50 ballik jarima.</param>
/// <param name="ShortSession">Sessiya 6 daqiqadan qisqa.</param>
/// <param name="TotalDurationSeconds">Sessiya davomiyligi (`Assessment.TotalDurationSeconds`), hisoblanmagan bo'lsa `null`.</param>
/// <param name="ReliabilityScore">Saqlangan ishonchlilik balli — hisoblanmagan bo'lsa `null` (0 EMAS).</param>
/// <param name="ReliabilityFlag">`Reliable` | `Questionable` | `Unreliable` yoki `null`.</param>
public sealed record AdminAnswerSessionSignalsDto(
    int AnsweredCount,
    int FastAnswerCount,
    int StraightLiningBlockCount,
    bool AllSameAnswer,
    bool ShortSession,
    int? TotalDurationSeconds,
    double? ReliabilityScore,
    string? ReliabilityFlag);

/// <summary>
/// Shkala darajasidagi teskari savol ziddiyati (`docs/03` §7.1 band 4) — FAQAT ikkala
/// yo'nalish ham mavjud bo'lgan shkalalar uchun. `ForwardAvgPct`/`ReverseAvgPct` —
/// normallashgan (0–100) o'rtachalar, `ReverseAvgPct` teskari TUZATILGAN qiymatlardan;
/// `MismatchPct` — ularning ayirmasi moduli, ya'ni hujjatdagi `d_shkala × 100`.
/// Jarima butun sessiya bo'yicha `d × 25` (o'rtacha) — bu yerda alohida shkalaning
/// hissasi ko'rsatiladi, jarimaning o'zi EMAS.
/// </summary>
public sealed record AdminAnswerScaleSignalDto(
    string Scale,
    string? ScaleNameUz,
    int ForwardCount,
    int ReverseCount,
    double ForwardAvgPct,
    double ReverseAvgPct,
    double MismatchPct);

/// <summary>
/// `ScoringConstants` dagi chegaralar — mijozga UZATILADI, mijozda qayta yozilmaydi
/// (`docs/03` §7; `CLAUDE.md` 3-band ruhi: chegara bitta manbada).
/// </summary>
public sealed record AdminAnswerThresholdsDto(
    int FastAnswerDurationMs,
    int StraightLiningMinRunLength,
    double ShortSessionMinutes);

/// <summary>
/// `POST /api/admin/assessments/{id}/recalculate-scores` javobi — `docs/07`da aniq namuna YO'Q.
/// Yangilangan natijalarni (`AdminTestResultsDto` — `Admin.Students`dagi bilan BIR XIL shakl,
/// `CLAUDE.md` 9-band) va ishonchlilikni qaytaradi; `Changed` — kamida bitta qiymat
/// (natija/ishonchlilik) eski qiymatdan farq qilganini bildiradi (audit/UI uchun qulaylik).
/// </summary>
public sealed record AdminRecalculateScoresResultDto(
    Guid AssessmentId,
    StudentRoadMap.Application.Admin.Students.AdminTestResultsDto Results,
    double? ReliabilityScore,
    string? ReliabilityFlag,
    bool Changed);
