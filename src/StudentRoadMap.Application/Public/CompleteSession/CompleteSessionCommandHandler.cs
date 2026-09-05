using MediatR;
using StudentRoadMap.Application.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Scoring;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Public.CompleteSession;

/// <summary>
/// `docs/07` 1.8-bo'lim + `docs/04` 2.3-bo'lim (holat mashinasi) + `docs/03` §3.3/§7 + `prompts/12`.
///
/// Oqim (faqat sessiya `InProgress`/`Draft`/`Abandoned` bo'lganda, ya'ni birinchi chaqiriqda):
/// 1. Barcha test bloklari tugagani (`Assessment.Complete` domen qo'riqchisi) tekshiriladi —
///    shu bilan birga `TotalDurationSeconds` hisoblanadi va `AssessmentCompletedEvent` chiqadi.
/// 2. `CompositeScorer.ApplyMaturityIndex` — BIG5 va ACTIVITY `TestResult`lari (agar ikkalasi
///    ham shu sessiyada mavjud bo'lsa) asosida `MaturityIndex` hisoblanadi va **BIG5**
///    `TestResult`iga yoziladi (`TestResult.ApplyCompositeIndex`).
/// 3. ⚠️ P12-R1: `ReliabilityInputBuilder.Build` orqali **xronologik** tartibda tuzilgan
///    `ReliabilityInput` bilan `ReliabilityCalculator.Calculate` — natija `Assessment.ReliabilityScore`/
///    `ReliabilityFlag`ga yoziladi.
/// 4. FAQAT `Ai:AutoAnalyzeOnCompletion` bayrog'i YOQILGAN bo'lsa: `Assessment.MarkAnalyzing`
///    va AI navbatiga qo'yish (`IBackgroundJobQueue`, `IPostCommitActions` orqali). Standart
///    qiymat `false` — tahlilni superadmin admin panelidagi "AI tahlil qilish" tugmasi
///    (`POST /api/admin/assessments/{id}/rerun-analysis`) bilan QO'LDA ishga tushiradi
///    (`docs/06` 8-bo'lim, 2026-09-03 egasi qarori: AI xarajati nazorati). Bayroq o'chiq
///    bo'lganda sessiya `Completed` holatida qoladi.
/// 5. `Student.UpdateSnapshot` — tip/indekslar/`NeedsAttention` yangilanadi.
///
/// **Idempotentlik** (`prompts/12` cheklovi): sessiya allaqachon `Completed`/`Analyzing`/
/// `Analyzed`/`AnalysisFailed` bo'lsa — yuqoridagi hech biri qayta bajarilmaydi, faqat joriy
/// holat asosida javob qaytariladi.
/// </summary>
internal sealed class CompleteSessionCommandHandler : IRequestHandler<CompleteSessionCommand, Result<CompleteSessionResult>>
{
    /// <summary>
    /// AI tahlili navbatga qo'yilganda — o'quvchi haqiqatan kutadi.
    /// </summary>
    private const string MessageAnalyzingUz = "Natijalaringiz qayta ishlanmoqda.";

    /// <summary>
    /// `Ai:AutoAnalyzeOnCompletion` o'chiq bo'lsa (2026-09-03 dan standart) hech narsa
    /// qayta ishlanmaydi — tahlil admin panelidagi tugma bilan boshlanadi. Bunday holatda
    /// "qayta ishlanmoqda" deyish o'quvchiga YOLG'ON: u natija o'zi paydo bo'lishini kutib
    /// turadi, aslida esa hech qanday jarayon yo'q.
    /// </summary>
    private const string MessageCompletedUz = "Javoblaringiz saqlandi. Rahmat!";

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IAppSettings _appSettings;
    private readonly IBackgroundJobQueue _backgroundJobQueue;
    private readonly IPostCommitActions _postCommitActions;

    public CompleteSessionCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IAppSettings appSettings,
        IBackgroundJobQueue backgroundJobQueue,
        IPostCommitActions postCommitActions)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _appSettings = appSettings;
        _backgroundJobQueue = backgroundJobQueue;
        _postCommitActions = postCommitActions;
    }

    public async Task<Result<CompleteSessionResult>> Handle(CompleteSessionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var assessment = await _executor.FirstOrDefaultAsync(
            _context.Assessments.Where(a => a.Id == request.AssessmentId),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<CompleteSessionResult>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        // P47: javobdagi `showResultToStudent` — GLOBAL kill-switch VA MAKON bayrog'ining
        // birlashmasi (`ShowResultPolicy`), faqat globalning o'zi EMAS. Aks holda javob
        // "natijangizni ko'rishingiz mumkin" deb va'da berib, `GET .../result` esa `403`
        // qaytarardi (maktab makonida bu HAR DOIM shunday bo'lardi).
        var spaceShowsResult = await _executor.AnyAsync(
            _context.AsNoTracking(_context.Schools).Where(s => s.Id == assessment.SchoolId && s.ShowResultToStudent),
            cancellationToken).ConfigureAwait(false);

        // Idempotentlik (`prompts/12`): sessiya allaqachon yakunlanish oqimidan o'tgan bo'lsa
        // (yoki AI hali/allaqachon ishlagan bo'lsa) — qayta hisoblanmaydi, joriy holat qaytadi.
        if (assessment.Status is AssessmentStatus.Completed or AssessmentStatus.Analyzing
            or AssessmentStatus.Analyzed or AssessmentStatus.AnalysisFailed)
        {
            return Result.Success(BuildResult(assessment, spaceShowsResult));
        }

        if (assessment.ExpiresAt <= now)
        {
            return Result.Failure<CompleteSessionResult>(new Error(ProblemCodes.SessionExpired, "Sessiyaning amal qilish muddati tugagan."));
        }

        // Fixup uchun: sessiyaga tegishli BARCHA test bloklarini SHU DbContext orqali tracked
        // yuklaymiz — `Assessment.Complete()` invarianti (barcha test `Completed`) shu
        // kolleksiya orqali tekshiriladi (`StartTestCommandHandler` izohidagi naqsh).
        var assessmentTests = await _executor.ToListAsync(
            _context.AssessmentTests.Where(t => t.AssessmentId == assessment.Id).OrderBy(t => t.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        // Domen: barcha test tugamagan bo'lsa `ASSESSMENT_TESTS_NOT_DONE` (409) bilan to'xtaydi;
        // aks holda `Status = Completed`, `CompletedAt`/`TotalDurationSeconds` hisoblanadi,
        // `AssessmentCompletedEvent` ko'taradi.
        assessment.Complete(now);

        // BIG5/ACTIVITY natijalari (agar ikkalasi ham shu sessiyada mavjud bo'lsa) — MaturityIndex.
        var testResults = await _executor.ToListAsync(
            _context.TestResults.Where(r => r.AssessmentId == assessment.Id),
            cancellationToken).ConfigureAwait(false);

        // ⚠️ Qaysi natija BIG5/ACTIVITY/MBTI16/RIASEC ekani metodika KODI bilan aniqlanmaydi
        // (`docs/06` 8-bo'lim, 2026-09-02 "dastur" qarori). Ilgari bu handler `TestCode == "BIG5"`
        // kabi satr solishtiruvi bilan ishlardi va `Custom` dasturda JIMGINA buzilardi: shu kodli
        // superadmin anketasi `CompositeScorer`ga BIG5 sifatida kirib ketardi (yoki aksincha,
        // boshqa kodli haqiqiy batareya e'tibordan chetda qolardi). Mezon — `PersonalityBattery.RoleOf`.
        var rolesByAssessmentTestId = await PersonalityBatteryRoles.LoadByAssessmentTestIdAsync(
            _context, _executor, assessment.Id, cancellationToken).ConfigureAwait(false);

        ApplyMaturityIndexIfPossible(testResults, rolesByAssessmentTestId);

        // P12-R1: ishonchlilik — xronologik tartibda tuzilgan `ReliabilityInput`. ⚠️ `Survey`
        // test bloklari (`nonSurveyAssessmentTestIds`) CHIQARIB TASHLANADI — nafaqat
        // `testBlocks` (savol ro'yxati), balki JAVOBLAR SO'ROVI HAM shu ro'yxat bilan
        // cheklanadi: `ReliabilityCalculator.CalculateFastAnswerPenalty` xom `answers`/
        // `durations` lug'atlaridan TO'G'RIDAN-TO'G'RI o'qiydi (`input.Questions` orqali
        // FILTRLANMAYDI) — agar Survey javoblari shu lug'atlarda qolib ketsa, ularning
        // duration'i (masalan o'ta tez) "FastAnswers" jarimasiga jimgina qo'shilib ketardi
        // (QA topilmasi: `PublicSurveyExcludedFromReliabilityEndpointTests` buni ushladi).
        var (testBlocks, nonSurveyAssessmentTestIds) = await BuildTestBlocksAsync(assessmentTests, cancellationToken).ConfigureAwait(false);

        var allAnswers = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Answers).Where(a => nonSurveyAssessmentTestIds.Contains(a.AssessmentTestId)),
            cancellationToken).ConfigureAwait(false);

        var answersById = allAnswers.ToDictionary(a => a.QuestionId, a => a.RawValue);
        var durationsById = allAnswers.ToDictionary(a => a.QuestionId, a => a.DurationMs);

        // `Assessment.Complete()` yuqorida `TotalDurationSeconds`ni allaqachon hisoblagan.
        var totalDuration = TimeSpan.FromSeconds(assessment.TotalDurationSeconds ?? 0);
        var reliabilityInput = ReliabilityInputBuilder.Build(testBlocks, answersById, durationsById, totalDuration);
        var reliabilityResult = ReliabilityCalculator.Calculate(reliabilityInput);

        assessment.SetReliability(reliabilityResult.Score, reliabilityResult.Flag, now);

        // AI navbati P18 da ulandi (`AnalysisJobQueue`, `Infrastructure/Jobs`), lekin
        // 2026-09-03 dan boshlab AVTOMATIK oqim `Ai:AutoAnalyzeOnCompletion` bayrog'i ostida
        // (standart `false`, `docs/06` 8-bo'lim: har tahlil AI xarajati — egasi kimni tahlil
        // qilishni admin panelidagi tugma orqali O'ZI tanlaydi). Bayroq o'chiq bo'lsa sessiya
        // `Completed` holatida QOLADI (`Analyzing` EMAS) — ballar, `TestResult`, ishonchlilik
        // va `StudentSnapshot` esa yuqoridagidek YAKUNLASH paytida hisoblanadi.
        var autoAnalyze = _appSettings.AutoAnalyzeOnCompletion;
        if (autoAnalyze)
        {
            assessment.MarkAnalyzing(now);
        }

        await UpdateStudentSnapshotAsync(assessment, testResults, rolesByAssessmentTestId, now, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (autoAnalyze)
        {
            // ⚠️ P18-R1 (MAJBURIY, bayroq YOQILGAN oqim uchun o'zgarishsiz): navbatga
            // qo'yish DARHOL emas — `TransactionBehavior` tranzaksiyasi muvaffaqiyatli commit
            // bo'lgandan KEYIN (`IPostCommitActions`). Aks holda fon ishchisi hali commit
            // qilinmagan `Assessment`ni o'qishga urinardi, yoki tranzaksiya rollback bo'lganda
            // mavjud bo'lmagan sessiya uchun vazifa navbatda qolib ketardi (P18-R2,
            // `IPostCommitActions` izohiga qarang).
            var assessmentId = assessment.Id;
            _postCommitActions.Enqueue(ct => _backgroundJobQueue.EnqueueAiAnalysisAsync(assessmentId, cancellationToken: ct));
        }

        return Result.Success(BuildResult(assessment, spaceShowsResult));
    }

    private CompleteSessionResult BuildResult(Assessment assessment, bool spaceShowsResult) =>
        new(
            assessment.Status.ToString(),
            assessment.Status == AssessmentStatus.Analyzing ? MessageAnalyzingUz : MessageCompletedUz,
            ShowResultPolicy.IsAllowed(_appSettings.ShowResultToStudent, spaceShowsResult),
            ResultAvailableAt: null);

    /// <summary>
    /// `CompositeScorer.ApplyMaturityIndex` (`Domain/Scoring`, o'zgartirilmaydi) chaqiradi —
    /// `Traits` (BIG5 strategiyasi) va `Activity` (ACTIVITY strategiyasi) ROLIDAGI `TestResult`lar
    /// `jsonb` ustunlaridan qayta o'qiladi (ular alohida
    /// `CompleteTestCommand` chaqiruvlarida, turli vaqtda yozilgan — bitta requestda ikkalasi
    /// ham xotirada bo'lishi mumkin emas). Faqat `CompositeScorer` talab qiladigan maydonlar
    /// (`NormalizedScores`) haqiqiy — qolganlari (`RawScores`/`Flags`/`InterpretationKey`)
    /// natijada ishlatilmagani uchun bo'sh/o'rnbosar qiymat bilan to'ldiriladi.
    /// </summary>
    private static void ApplyMaturityIndexIfPossible(
        IReadOnlyList<TestResult> testResults,
        IReadOnlyDictionary<Guid, PersonalityBatteryRole> rolesByAssessmentTestId)
    {
        var bigFive = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.Traits);
        var activity = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.Activity);

        if (bigFive is null || activity is null)
        {
            return;
        }

        var bigFiveScoringResult = new ScoringResult(
            ResultCode: bigFive.ResultCode,
            RawScores: TestResultJson.DeserializeScores(bigFive.RawScoresJson),
            NormalizedScores: TestResultJson.DeserializeScores(bigFive.NormalizedScoresJson),
            Levels: TestResultJson.DeserializeLevels(bigFive.LevelsJson),
            CompositeIndex: bigFive.CompositeIndex,
            Flags: TestResultJson.DeserializeFlags(bigFive.FlagsJson),
            InterpretationKey: "BIG5.RESULT",
            ScoringVersion: bigFive.ScoringVersion);

        var activityScoringResult = new ScoringResult(
            ResultCode: activity.ResultCode,
            RawScores: TestResultJson.DeserializeScores(activity.RawScoresJson),
            NormalizedScores: TestResultJson.DeserializeScores(activity.NormalizedScoresJson),
            Levels: TestResultJson.DeserializeLevels(activity.LevelsJson),
            CompositeIndex: activity.CompositeIndex,
            Flags: TestResultJson.DeserializeFlags(activity.FlagsJson),
            InterpretationKey: "ACTIVITY.RESULT",
            ScoringVersion: activity.ScoringVersion);

        var updated = CompositeScorer.ApplyMaturityIndex(bigFiveScoringResult, activityScoringResult);
        if (updated.IsFailure)
        {
            // BIG5/ACTIVITY mavjud, lekin kutilgan shkalalar yo'q — bu seed/scoring
            // konfiguratsiyasi xatosi (ommaviy API foydalanuvchisi sabab emas), boshqa domen
            // xatolari kabi `ExceptionHandlingMiddleware`ga uzatiladi.
            throw new DomainException(updated.Error.Code, updated.Error.Message);
        }

        bigFive.ApplyCompositeIndex(updated.Value.CompositeIndex!.Value, TestResultJson.Serialize(updated.Value.Levels));
    }

    /// <summary>
    /// Har bir sessiya test bloki uchun (sessiya ICHIDAGI tartib + faol savollar metama'lumoti) —
    /// `ReliabilityInputBuilder.Build` kirishi. ⚠️ `Survey` (`docs/06` 8-bo'lim, `prompts/34`
    /// D-band) test bloklari BU YERDA chetlab o'tiladi — ballanmagan javobda teskira savol
    /// tushunchasi yo'q, `ReliabilityCalculator`ga berish ballni buzadi (P12-R1 ruhida).
    /// `NonSurveyAssessmentTestIds` — chaqiruvchi (`Handle`) shu ro'yxat bilan JAVOBLAR
    /// so'rovini ham cheklashi shart (`CalculateFastAnswerPenalty` xom `answers`/`durations`
    /// lug'atlaridan `input.Questions`siz o'qiydi — filtrlanmagan lug'at Survey javoblarini
    /// jimgina jarima hisobiga qo'shib qo'yardi).
    /// </summary>
    private async Task<(IReadOnlyList<ReliabilityInputBuilder.TestBlock> TestBlocks, IReadOnlyList<Guid> NonSurveyAssessmentTestIds)> BuildTestBlocksAsync(
        IReadOnlyList<AssessmentTest> assessmentTests,
        CancellationToken cancellationToken)
    {
        var testDefinitionIds = assessmentTests.Select(t => t.TestDefinitionId).ToList();

        var scoringModeByTestDefinitionId = (await _executor.ToListAsync(
                _context.AsNoTracking(_context.TestDefinitions)
                    .Where(t => testDefinitionIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.ScoringMode }),
                cancellationToken).ConfigureAwait(false))
            .ToDictionary(x => x.Id, x => x.ScoringMode);

        var surveyExcludedAssessmentTests = assessmentTests
            .Where(t => scoringModeByTestDefinitionId.GetValueOrDefault(t.TestDefinitionId) != TestScoringMode.Survey)
            .ToList();

        var questions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions).Where(q => testDefinitionIds.Contains(q.TestDefinitionId) && q.IsActive),
            cancellationToken).ConfigureAwait(false);

        var questionsByTestDefinitionId = questions
            .GroupBy(q => q.TestDefinitionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var testBlocks = surveyExcludedAssessmentTests
            .Select(t => new ReliabilityInputBuilder.TestBlock(
                t.DisplayOrder,
                (questionsByTestDefinitionId.TryGetValue(t.TestDefinitionId, out var testQuestions) ? testQuestions : [])
                    .Select(q => new QuestionMeta(q.Id, q.Code, q.Scale, q.ScaleDirection, q.Weight, q.QuestionType, q.DisplayOrder))
                    .ToList()))
            .ToList();

        var nonSurveyAssessmentTestIds = surveyExcludedAssessmentTests.Select(t => t.Id).ToList();

        return (testBlocks, nonSurveyAssessmentTestIds);
    }

    /// <summary>
    /// `Student.UpdateSnapshot` (mavjud domen metodi) orqali tip/indekslar/`NeedsAttention`
    /// yangilanadi (`prompts/12` cheklovi 6). Har bir natija BATAREYA ROLI bo'yicha topiladi
    /// (`PersonalityBattery.RoleOf`, metodika kodi bo'yicha EMAS): shu sessiyada mos rol
    /// bo'lmasa (masalan dasturda batareya yo'q yoki test to'plami boshqacha) mos maydon
    /// `null` qoladi — snapshot bo'sh emas, faqat mavjud ma'lumot bilan yangilanadi.
    /// </summary>
    private async Task UpdateStudentSnapshotAsync(
        Assessment assessment,
        IReadOnlyList<TestResult> testResults,
        IReadOnlyDictionary<Guid, PersonalityBatteryRole> rolesByAssessmentTestId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var student = await _executor.FirstOrDefaultAsync(
            _context.Students.Where(s => s.Id == assessment.StudentId),
            cancellationToken).ConfigureAwait(false);

        if (student is null)
        {
            // Nazariy jihatdan bo'lishi mumkin emas (Assessment.StudentId har doim mavjud
            // Student'ga ishora qiladi) — jimgina o'tkazib yuborish sessiyani yakunlashni
            // to'xtatmaydi, faqat snapshot yangilanmaydi.
            return;
        }

        var mbti = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.PersonalityType);
        var bigFive = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.Traits);
        var activity = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.Activity);
        var riasec = PersonalityBatteryRoles.FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.CareerInterest);

        ActivityLevel? activityLevel = null;
        var needsAttention = student.NeedsAttention;
        if (activity is not null)
        {
            // ⚠️ Bu yerdagi `"ACTIVITY"` — metodika KODI emas, `ActivityStrategy` yozadigan
            // `Levels` lug'atining kaliti (`docs/03` §5.2 natija shartnomasi). Natijaning O'ZI
            // yuqorida ROL bo'yicha topilgan, ya'ni uni aynan `ACTIVITY` strategiyasi hisoblagani
            // allaqachon kafolatlangan — kalit shu strategiyaning chiqish shartnomasidan o'qiladi.
            var levels = TestResultJson.DeserializeLevels(activity.LevelsJson);
            if (levels.TryGetValue("ACTIVITY", out var levelCode) && Enum.TryParse<ActivityLevel>(levelCode, out var parsedLevel))
            {
                activityLevel = parsedLevel;
            }

            needsAttention = TestResultJson.DeserializeFlags(activity.FlagsJson).Contains("NeedsAttention");
        }

        // ⚠️ P34 D-band ("ENG NOZIK #2"): ma'lumotsiz sessiya eski qiymatni O'CHIRMAYDI —
        // shu sessiyada mos test bo'lmasa (masalan faqat `Survey` yoki faqat RIASEC dasturi),
        // o'sha maydon `student`ning OLDINGI qiymatida qoladi (`?? student.Last...`). `null`
        // faqat student HALIGACHA hech qachon mos ma'lumotga ega bo'lmagan bo'lsa qaytadi.
        // `bigFive?.CompositeIndex` ham shu qoidaga tabiiy bo'ysunadi: BIG5 bor-u ACTIVITY yo'q
        // bo'lsa `CompositeIndex` (MaturityIndex) hisoblanmagan (`ApplyMaturityIndexIfPossible`)
        // — demak `null`, va shu yerda eski qiymatga qaytadi (0 emas!).
        student.UpdateSnapshot(
            lastPersonalityType: mbti?.ResultCode ?? student.LastPersonalityType,
            lastMaturityIndex: bigFive?.CompositeIndex ?? student.LastMaturityIndex,
            lastActivityIndex: activity?.CompositeIndex ?? student.LastActivityIndex,
            lastActivityLevel: activityLevel ?? student.LastActivityLevel,
            lastHollandCode: riasec?.ResultCode ?? student.LastHollandCode,
            needsAttention: needsAttention,
            lastAssessmentAt: now,
            completedAssessmentCount: student.CompletedAssessmentCount + 1,
            now: now);
    }
}
