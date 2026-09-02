using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
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
/// 4. `Assessment.MarkAnalyzing` — AI navbatga qo'yiladi (`IBackgroundJobQueue`, hozircha `NoOpJobQueue`).
/// 5. `Student.UpdateSnapshot` — tip/indekslar/`NeedsAttention` yangilanadi.
///
/// **Idempotentlik** (`prompts/12` cheklovi): sessiya allaqachon `Completed`/`Analyzing`/
/// `Analyzed`/`AnalysisFailed` bo'lsa — yuqoridagi hech biri qayta bajarilmaydi, faqat joriy
/// holat asosida javob qaytariladi.
/// </summary>
internal sealed class CompleteSessionCommandHandler : IRequestHandler<CompleteSessionCommand, Result<CompleteSessionResult>>
{
    private const string MessageUz = "Natijalaringiz qayta ishlanmoqda.";

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IAppSettings _appSettings;
    private readonly IBackgroundJobQueue _backgroundJobQueue;

    public CompleteSessionCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IAppSettings appSettings,
        IBackgroundJobQueue backgroundJobQueue)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _appSettings = appSettings;
        _backgroundJobQueue = backgroundJobQueue;
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

        // Idempotentlik (`prompts/12`): sessiya allaqachon yakunlanish oqimidan o'tgan bo'lsa
        // (yoki AI hali/allaqachon ishlagan bo'lsa) — qayta hisoblanmaydi, joriy holat qaytadi.
        if (assessment.Status is AssessmentStatus.Completed or AssessmentStatus.Analyzing
            or AssessmentStatus.Analyzed or AssessmentStatus.AnalysisFailed)
        {
            return Result.Success(BuildResult(assessment));
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

        ApplyMaturityIndexIfPossible(testResults);

        // P12-R1: ishonchlilik — xronologik tartibda tuzilgan `ReliabilityInput`.
        var testBlocks = await BuildTestBlocksAsync(assessmentTests, cancellationToken).ConfigureAwait(false);

        var assessmentTestIds = assessmentTests.Select(t => t.Id).ToList();
        var allAnswers = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Answers).Where(a => assessmentTestIds.Contains(a.AssessmentTestId)),
            cancellationToken).ConfigureAwait(false);

        var answersById = allAnswers.ToDictionary(a => a.QuestionId, a => a.RawValue);
        var durationsById = allAnswers.ToDictionary(a => a.QuestionId, a => a.DurationMs);

        // `Assessment.Complete()` yuqorida `TotalDurationSeconds`ni allaqachon hisoblagan.
        var totalDuration = TimeSpan.FromSeconds(assessment.TotalDurationSeconds ?? 0);
        var reliabilityInput = ReliabilityInputBuilder.Build(testBlocks, answersById, durationsById, totalDuration);
        var reliabilityResult = ReliabilityCalculator.Calculate(reliabilityInput);

        assessment.SetReliability(reliabilityResult.Score, reliabilityResult.Flag, now);

        // AI navbati P18 da ulanadi — hozircha `NoOpJobQueue` (`prompts/12`).
        assessment.MarkAnalyzing(now);

        await UpdateStudentSnapshotAsync(assessment, testResults, now, cancellationToken).ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _backgroundJobQueue.EnqueueAiAnalysisAsync(assessment.Id, cancellationToken).ConfigureAwait(false);

        return Result.Success(BuildResult(assessment));
    }

    private CompleteSessionResult BuildResult(Assessment assessment) =>
        new(assessment.Status.ToString(), MessageUz, _appSettings.ShowResultToStudent, ResultAvailableAt: null);

    /// <summary>
    /// `CompositeScorer.ApplyMaturityIndex` (`Domain/Scoring`, o'zgartirilmaydi) chaqiradi —
    /// BIG5/ACTIVITY `TestResult`lari `jsonb` ustunlaridan qayta o'qiladi (ular alohida
    /// `CompleteTestCommand` chaqiruvlarida, turli vaqtda yozilgan — bitta requestda ikkalasi
    /// ham xotirada bo'lishi mumkin emas). Faqat `CompositeScorer` talab qiladigan maydonlar
    /// (`NormalizedScores`) haqiqiy — qolganlari (`RawScores`/`Flags`/`InterpretationKey`)
    /// natijada ishlatilmagani uchun bo'sh/o'rnbosar qiymat bilan to'ldiriladi.
    /// </summary>
    private static void ApplyMaturityIndexIfPossible(IReadOnlyList<TestResult> testResults)
    {
        var bigFive = testResults.FirstOrDefault(r => r.TestCode == "BIG5");
        var activity = testResults.FirstOrDefault(r => r.TestCode == "ACTIVITY");

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

    /// <summary>Har bir sessiya test bloki uchun (sessiya ICHIDAGI tartib + faol savollar metama'lumoti) — `ReliabilityInputBuilder.Build` kirishi.</summary>
    private async Task<IReadOnlyList<ReliabilityInputBuilder.TestBlock>> BuildTestBlocksAsync(
        IReadOnlyList<AssessmentTest> assessmentTests,
        CancellationToken cancellationToken)
    {
        var testDefinitionIds = assessmentTests.Select(t => t.TestDefinitionId).ToList();
        var questions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions).Where(q => testDefinitionIds.Contains(q.TestDefinitionId) && q.IsActive),
            cancellationToken).ConfigureAwait(false);

        var questionsByTestDefinitionId = questions
            .GroupBy(q => q.TestDefinitionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return assessmentTests
            .Select(t => new ReliabilityInputBuilder.TestBlock(
                t.DisplayOrder,
                (questionsByTestDefinitionId.TryGetValue(t.TestDefinitionId, out var testQuestions) ? testQuestions : [])
                    .Select(q => new QuestionMeta(q.Id, q.Code, q.Scale, q.ScaleDirection, q.Weight, q.QuestionType, q.DisplayOrder))
                    .ToList()))
            .ToList();
    }

    /// <summary>
    /// `Student.UpdateSnapshot` (mavjud domen metodi) orqali tip/indekslar/`NeedsAttention`
    /// yangilanadi (`prompts/12` cheklovi 6). `MBTI16`/`RIASEC` shu sessiyada bo'lmasa (masalan
    /// test to'plami boshqacha bo'lsa) mos maydon `null` qoladi — snapshot bo'sh emas, faqat
    /// mavjud ma'lumot bilan yangilanadi.
    /// </summary>
    private async Task UpdateStudentSnapshotAsync(
        Assessment assessment,
        IReadOnlyList<TestResult> testResults,
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

        var mbti = testResults.FirstOrDefault(r => r.TestCode == "MBTI16");
        var bigFive = testResults.FirstOrDefault(r => r.TestCode == "BIG5");
        var activity = testResults.FirstOrDefault(r => r.TestCode == "ACTIVITY");
        var riasec = testResults.FirstOrDefault(r => r.TestCode == "RIASEC");

        ActivityLevel? activityLevel = null;
        var needsAttention = false;
        if (activity is not null)
        {
            var levels = TestResultJson.DeserializeLevels(activity.LevelsJson);
            if (levels.TryGetValue("ACTIVITY", out var levelCode) && Enum.TryParse<ActivityLevel>(levelCode, out var parsedLevel))
            {
                activityLevel = parsedLevel;
            }

            needsAttention = TestResultJson.DeserializeFlags(activity.FlagsJson).Contains("NeedsAttention");
        }

        student.UpdateSnapshot(
            lastPersonalityType: mbti?.ResultCode,
            lastMaturityIndex: bigFive?.CompositeIndex,
            lastActivityIndex: activity?.CompositeIndex,
            lastActivityLevel: activityLevel,
            lastHollandCode: riasec?.ResultCode,
            needsAttention: needsAttention,
            lastAssessmentAt: now,
            completedAssessmentCount: student.CompletedAssessmentCount + 1,
            now: now);
    }
}
