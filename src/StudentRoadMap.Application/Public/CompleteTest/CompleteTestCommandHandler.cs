using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Application.Public.CompleteTest;

/// <summary>
/// `docs/07` 1.7-bo'lim + `docs/03` §8 (scoring engine shartnomasi) + `prompts/12`.
///
/// Oqim:
/// 1. Sessiya (tracked) topiladi, muddati tekshiriladi.
/// 2. Nishonlangan test bloki (fixup, `StartTestCommandHandler` naqshi) topiladi.
/// 3. **Idempotent**: allaqachon `Completed` bo'lsa — qayta hisoblanmaydi, faqat javob
///    (`nextTestCode`/`allTestsCompleted`) qaytariladi (mijoz tarmoq xatosidan keyin qayta
///    yuborishi mumkin — `prompts/12` `CompleteSession` uchun talab qilgan idempotentlikning
///    tabiiy davomi, aks holda ikkinchi chaqiriqda `ASSESSMENT_TEST_INVALID_TRANSITION` 409
///    berardi).
/// 4. Aks holda: majburiy savollar soni tekshiriladi (`unansweredCount` bilan `400`), keyin
///    `Assessment.CompleteTest` (domen) chaqiriladi — `AssessmentTest.Status = Completed`,
///    `TestCompletedEvent` ko'taradi (SaveChanges paytida avtomatik chiqadi, `AppDbContext`
///    §"PublishDomainEventsAsync"), va **sinxron** `ScoringEngine` bilan hisoblanib `TestResult`
///    yoziladi.
/// </summary>
internal sealed class CompleteTestCommandHandler : IRequestHandler<CompleteTestCommand, Result<CompleteTestResult>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly PublicCatalogCache _catalogCache;
    private readonly ScoringEngine _scoringEngine;

    public CompleteTestCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        PublicCatalogCache catalogCache,
        ScoringEngine scoringEngine)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _catalogCache = catalogCache;
        _scoringEngine = scoringEngine;
    }

    public async Task<Result<CompleteTestResult>> Handle(CompleteTestCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var assessment = await _executor.FirstOrDefaultAsync(
            _context.Assessments.Where(a => a.Id == request.AssessmentId),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<CompleteTestResult>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        if (assessment.ExpiresAt <= now)
        {
            return Result.Failure<CompleteTestResult>(new Error(ProblemCodes.SessionExpired, "Sessiyaning amal qilish muddati tugagan."));
        }

        var testDefinition = await _catalogCache.GetPublishedTestDefinitionAsync(request.TestCode, cancellationToken).ConfigureAwait(false);
        if (testDefinition is null)
        {
            return Result.Failure<CompleteTestResult>(new Error(ProblemCodes.NotFound, "Test topilmadi."));
        }

        // Fixup uchun: shu sessiyaga tegishli BARCHA test bloklarini SHU DbContext orqali
        // tracked holda yuklaymiz (`StartTestCommandHandler` izohiga qarang).
        var assessmentTests = await _executor.ToListAsync(
            _context.AssessmentTests.Where(t => t.AssessmentId == assessment.Id).OrderBy(t => t.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var targetTest = assessmentTests.FirstOrDefault(t => t.TestDefinitionId == testDefinition.Id);
        if (targetTest is null)
        {
            return Result.Failure<CompleteTestResult>(new Error(ProblemCodes.NotFound, "Bu test ushbu sessiyaga biriktirilmagan."));
        }

        if (targetTest.Status != TestStatus.Completed)
        {
            // Fixup: shu testning javoblarini SHU DbContext orqali tracked yuklaymiz — bu
            // ro'yxat pastda ham "majburiy savol javoblanganmi" tekshiruvi, ham scoring uchun
            // ishlatiladi (`SaveAnswersCommandHandler`dagi fixup izohiga qarang).
            _ = await _executor.ToListAsync(
                _context.Answers.Where(a => a.AssessmentTestId == targetTest.Id),
                cancellationToken).ConfigureAwait(false);

            // Savollar (`IsRequired` bilan) — QA tuzatmasi (`prompts/12`): yakunlash tekshiruvi
            // faqat MAJBURIY savollarni hisobga oladi, `TotalCount` (barcha faol savol, progress
            // ko'rsatkichi) EMAS. Bir marta yuklanadi — pastda `ScoreAsync`ga ham uzatiladi
            // (ikkinchi so'rov shart emas).
            var questions = await _executor.ToListAsync(
                _context.AsNoTracking(_context.Questions).Where(q => q.TestDefinitionId == testDefinition.Id && q.IsActive),
                cancellationToken).ConfigureAwait(false);

            var requiredQuestionIds = questions.Where(q => q.IsRequired).Select(q => q.Id).ToList();
            var answeredQuestionIds = targetTest.Answers.Select(a => a.QuestionId).ToHashSet();
            var unansweredCount = requiredQuestionIds.Count(id => !answeredQuestionIds.Contains(id));

            if (unansweredCount > 0)
            {
                return Result.Failure<CompleteTestResult>(new Error(
                    ProblemCodes.ValidationError,
                    "Barcha majburiy savollarga javob berilmasdan testni yakunlab bo'lmaydi.",
                    new Dictionary<string, object> { ["unansweredCount"] = unansweredCount }));
            }

            // Domen: `AssessmentTest.Status = Completed`, `TestCompletedEvent` ko'taradi.
            // `requiredQuestionIds` domenga ham uzatiladi — `AssessmentTest.Complete` invarianti
            // shu bilan izchil (ixtiyoriy savol javobsiz qolsa ham domen bloklamasin).
            assessment.CompleteTest(testDefinition.Id, requiredQuestionIds, now);

            // `Survey` (`docs/06` 8-bo'lim, 2026-09-02 qaror, `prompts/34` D-band): javoblari
            // saqlanadi (yuqorida), lekin `TestResult` ball YOZILMAYDI — ballanmaydi, scoring
            // va AI xulosasiga ta'sir qilmaydi, `ReliabilityCalculator` kirishiga kirmaydi
            // (`CompleteSessionCommandHandler.BuildTestBlocksAsync` shu testni chetlab o'tadi).
            if (testDefinition.ScoringMode != TestScoringMode.Survey)
            {
                var scoringResult = await ScoreAsync(testDefinition, targetTest, questions, cancellationToken).ConfigureAwait(false);

                var testResult = TestResult.Create(
                    Guid.NewGuid(),
                    targetTest.Id,
                    assessment.Id,
                    testDefinition.Code,
                    TestResultJson.Serialize(scoringResult.RawScores),
                    TestResultJson.Serialize(scoringResult.NormalizedScores),
                    scoringResult.ScoringVersion,
                    testDefinition.Version,
                    now,
                    resultCode: scoringResult.ResultCode,
                    levelsJson: TestResultJson.Serialize(scoringResult.Levels),
                    compositeIndex: scoringResult.CompositeIndex,
                    flagsJson: TestResultJson.SerializeFlags(scoringResult.Flags));

                _context.Add(testResult);
            }

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var nextTest = assessmentTests
            .Where(t => t.DisplayOrder > targetTest.DisplayOrder)
            .OrderBy(t => t.DisplayOrder)
            .FirstOrDefault();

        string? nextTestCode = null;
        if (nextTest is not null)
        {
            nextTestCode = await _executor.FirstOrDefaultAsync(
                _context.AsNoTracking(_context.TestDefinitions)
                    .Where(t => t.Id == nextTest.TestDefinitionId)
                    .Select(t => t.Code),
                cancellationToken).ConfigureAwait(false);
        }

        var allTestsCompleted = assessmentTests.All(t => t.Status == TestStatus.Completed);

        var result = new CompleteTestResult(testDefinition.Code, TestStatus.Completed.ToString(), nextTestCode, allTestsCompleted);

        return Result.Success(result);
    }

    /// <summary>
    /// `ScoringEngine.Score(strategyCode, ...)` uchun kirish tayyorlaydi — savollar (`Scale`/
    /// `Direction`/`Weight` bilan, katalog keshi bu maydonlarni ATAYLAB tashimaydi, `CLAUDE.md`
    /// 9-qoida) va sinov javoblari to'g'ridan-to'g'ri `Domain.Catalog.Question`dan o'qiladi.
    /// `StudentContext` hozircha bo'sh — P09 strategiyalari yosh/sinf/jinsdan foydalanmaydi
    /// (`StudentContext.cs` izohi: "kelajakda... AI uchun kiritilgan").
    /// </summary>
    private async Task<ScoringResult> ScoreAsync(
        CachedTestDefinitionDto testDefinition,
        AssessmentTest assessmentTest,
        IReadOnlyList<Question> questions,
        CancellationToken cancellationToken)
    {
        var questionMetas = questions
            .Select(q => new QuestionMeta(q.Id, q.Code, q.Scale, q.ScaleDirection, q.Weight, q.QuestionType, q.DisplayOrder))
            .ToList();

        var answers = assessmentTest.Answers.ToDictionary(a => a.QuestionId, a => a.RawValue);
        var durations = assessmentTest.Answers.ToDictionary(a => a.QuestionId, a => a.DurationMs);

        var scoringInput = new ScoringInput(questionMetas, answers, durations, new StudentContext(null, null, null));

        // `ScoringEngine.Score` faqat "strategiya topilmadi" holatini `Result.Failure` bilan
        // qaytaradi (`docs/03` §8 kontraktidagi tayyor sinf) — strategiya ICHIDAGI xatolar
        // (masalan noto'g'ri savol soni) `DomainException` sifatida to'g'ridan-to'g'ri otiladi
        // va `ExceptionHandlingMiddleware` ushlaydi (loyihadagi mavjud konvensiya). Bu yerda
        // faqat "strategiya topilmadi" holatini qayta ishlaymiz — u seed konfiguratsiyasi
        // xatosini bildiradi, ommaviy API foydalanuvchisiga aloqasi yo'q, shuning uchun
        // `DomainException`ga aylantirib xuddi shu yo'l bilan yuqoriga uzatiladi.
        // Faqat `ScoringMode != Survey` bo'lganda chaqiriladi (yuqoridagi `Handle` sharti) — shu
        // sabab `ScoringStrategyCode` bu yerda amalda har doim mavjud.
        var result = _scoringEngine.Score(testDefinition.ScoringStrategyCode ?? string.Empty, scoringInput);
        if (result.IsFailure)
        {
            throw new DomainException(result.Error.Code, result.Error.Message);
        }

        return result.Value;
    }
}
