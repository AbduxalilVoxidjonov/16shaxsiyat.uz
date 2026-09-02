using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Application.Admin.Assessments.RecalculateScores;

/// <summary>
/// `docs/07` 3.3-bo'lim, `prompts/15` "⚠️ ENG MUHIM" va MAXSUS DIQQAT #6.
///
/// Oqim — `CompleteTestCommandHandler`/`CompleteSessionCommandHandler` (`Public/`, P12) bilan
/// BIR XIL scoring mantig'ini qayta ishlatadi, lekin O'QUVCHI OQIMI EMAS: sessiya holat
/// mashinasi (`Assessment.Status`) O'ZGARMAYDI, faqat mavjud `Answer`lardan (o'zgarmagan)
/// `TestResult.ReplaceScores` bilan natijalar va `Assessment.ReliabilityScore`/`ReliabilityFlag`
/// qayta hisoblanadi:
/// 1. Har `TestResult`i BOR (ya'ni yakunlangan) test bloki uchun `ScoringEngine.Score` —
///    saqlangan javoblar bilan.
/// 2. BIG5/ACTIVITY ikkalasi ham bo'lsa — `CompositeScorer.ApplyMaturityIndex` qayta.
/// 3. ⚠️ P12-R1 (MAJBURIY): `ReliabilityInputBuilder.Build` orqali — butun ro'yxatga
///    `.OrderBy(q => q.DisplayOrder)` QOʻLLANMAYDI (`prompts/15` "ENG MUHIM" bandi).
///
/// **Idempotentlik** (MAXSUS DIQQAT #6): sof, deterministik hisoblash (`ADR-6`) — javoblar
/// o'zgarmasa, ikki ketma-ket chaqiruv BIR XIL natija beradi (`RecalculateAssessmentScores...Tests`
/// integratsiya sinovida tekshiriladi).
/// </summary>
internal sealed class RecalculateAssessmentScoresCommandHandler
    : IRequestHandler<RecalculateAssessmentScoresCommand, Result<AdminRecalculateScoresResultDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly ScoringEngine _scoringEngine;

    public RecalculateAssessmentScoresCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IIpHasher ipHasher,
        ScoringEngine scoringEngine)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _scoringEngine = scoringEngine;
    }

    public async Task<Result<AdminRecalculateScoresResultDto>> Handle(RecalculateAssessmentScoresCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var assessment = await _executor.FirstOrDefaultAsync(
            _context.Assessments.Where(a => a.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<AdminRecalculateScoresResultDto>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        var assessmentTests = await _executor.ToListAsync(
            _context.AssessmentTests.Where(t => t.AssessmentId == assessment.Id).OrderBy(t => t.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        // Tracked — pastda `ReplaceScores`/`ApplyCompositeIndex` bilan o'zgartiriladi.
        var testResults = await _executor.ToListAsync(
            _context.TestResults.Where(r => r.AssessmentId == assessment.Id),
            cancellationToken).ConfigureAwait(false);

        if (testResults.Count == 0)
        {
            // Hali birorta ham test bloki yakunlanmagan (`Draft`/`InProgress`/`Abandoned`) —
            // qayta hisoblash uchun asos yo'q.
            return Result.Failure<AdminRecalculateScoresResultDto>(new Error(
                ProblemCodes.ValidationError,
                "Sessiyada hisoblangan natijalar yo'q — qayta hisoblab bo'lmaydi."));
        }

        // "before" — `changed` bayrog'i uchun TO'LIQ payload (QA topilmasi, 2026-09-02: avval
        // faqat `ResultCode`/`CompositeIndex` solishtirilardi — scoring versiyasi o'zgarib xom/
        // normallashgan ballar boshqacha chiqsa-yu, natija kodi/kompozit indeks o'zgarmasa,
        // `changed=false` noto'g'ri qaytardi). Audit yozuviga esa hamon FAQAT `ResultCode`/
        // `CompositeIndex` ketadi (`beforeSnapshot`/`afterSnapshot`, pastda) — shaxsiy ma'lumot
        // yo'q talabi (`prompts/15` MAXSUS DIQQAT #6) shu bilan ta'minlanadi, ballarning o'zi
        // shaxsiy ma'lumot emas, lekin audit yozuvini keraksiz shishirmaslik uchun qisqa qoldirilgan.
        // Qiymatlar (string/double?/int) darhol nusxalanadi — pastda xuddi shu `TestResult`
        // obyektlari joyida o'zgartiriladi.
        var before = testResults.ToDictionary(r => r.TestCode, r => (
            r.ResultCode,
            r.RawScoresJson,
            r.NormalizedScoresJson,
            r.LevelsJson,
            r.FlagsJson,
            r.ScoringVersion,
            r.CompositeIndex));
        var oldReliabilityScore = assessment.ReliabilityScore;
        var oldReliabilityFlag = assessment.ReliabilityFlag;

        var testDefinitionIds = assessmentTests.Select(t => t.TestDefinitionId).Distinct().ToList();
        var testDefinitions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => testDefinitionIds.Contains(t.Id)),
            cancellationToken).ConfigureAwait(false);
        var testDefinitionById = testDefinitions.ToDictionary(t => t.Id);

        var questions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions).Where(q => testDefinitionIds.Contains(q.TestDefinitionId) && q.IsActive),
            cancellationToken).ConfigureAwait(false);
        var questionsByTestDefinitionId = questions.GroupBy(q => q.TestDefinitionId).ToDictionary(g => g.Key, g => g.ToList());

        var assessmentTestIds = assessmentTests.Select(t => t.Id).ToList();
        var allAnswers = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Answers).Where(a => assessmentTestIds.Contains(a.AssessmentTestId)),
            cancellationToken).ConfigureAwait(false);
        var answersByAssessmentTestId = allAnswers.GroupBy(a => a.AssessmentTestId).ToDictionary(g => g.Key, g => g.ToList());

        var testResultByAssessmentTestId = testResults.ToDictionary(r => r.AssessmentTestId);

        foreach (var assessmentTest in assessmentTests)
        {
            if (!testResultByAssessmentTestId.TryGetValue(assessmentTest.Id, out var testResult))
            {
                continue; // bu test bloki hali yakunlanmagan — natijasi yo'q, tegilmaydi.
            }

            if (!testDefinitionById.TryGetValue(assessmentTest.TestDefinitionId, out var testDefinition))
            {
                continue; // FK bo'yicha bo'lmasligi kerak — himoya.
            }

            var questionMetas = questionsByTestDefinitionId.GetValueOrDefault(assessmentTest.TestDefinitionId, [])
                .Select(q => new QuestionMeta(q.Id, q.Code, q.Scale, q.ScaleDirection, q.Weight, q.QuestionType, q.DisplayOrder))
                .ToList();

            var testAnswers = answersByAssessmentTestId.GetValueOrDefault(assessmentTest.Id, []);
            var answersDict = testAnswers.ToDictionary(a => a.QuestionId, a => a.RawValue);
            var durationsDict = testAnswers.ToDictionary(a => a.QuestionId, a => a.DurationMs);

            var scoringInput = new ScoringInput(questionMetas, answersDict, durationsDict, new StudentContext(null, null, null));
            var scoringResult = _scoringEngine.Score(testDefinition.ScoringStrategyCode, scoringInput);
            if (scoringResult.IsFailure)
            {
                // Seed/konfiguratsiya xatosi (strategiya topilmadi) — `CompleteTestCommandHandler`
                // bilan bir xil yo'l: `DomainException`, `ExceptionHandlingMiddleware` ushlaydi.
                throw new DomainException(scoringResult.Error.Code, scoringResult.Error.Message);
            }

            var value = scoringResult.Value;
            testResult.ReplaceScores(
                value.ResultCode,
                TestResultJson.Serialize(value.RawScores),
                TestResultJson.Serialize(value.NormalizedScores),
                TestResultJson.Serialize(value.Levels),
                value.CompositeIndex,
                TestResultJson.SerializeFlags(value.Flags),
                value.ScoringVersion,
                now);
        }

        ApplyMaturityIndexIfPossible(testResults);

        // ⚠️ P12-R1 (MAJBURIY, `prompts/15` "ENG MUHIM"): `ReliabilityInputBuilder.Build` orqali
        // — sessiya ICHIDAGI test tartibi (`AssessmentTest.DisplayOrder`) + har test ICHIDAGI
        // savol tartibi. Butun ro'yxatga bittalikda `.OrderBy(q => q.DisplayOrder)` TAQIQLANGAN.
        var testBlocks = assessmentTests
            .Select(t => new ReliabilityInputBuilder.TestBlock(
                t.DisplayOrder,
                questionsByTestDefinitionId.GetValueOrDefault(t.TestDefinitionId, [])
                    .Select(q => new QuestionMeta(q.Id, q.Code, q.Scale, q.ScaleDirection, q.Weight, q.QuestionType, q.DisplayOrder))
                    .ToList()))
            .ToList();

        var reliabilityAnswers = allAnswers.ToDictionary(a => a.QuestionId, a => a.RawValue);
        var reliabilityDurations = allAnswers.ToDictionary(a => a.QuestionId, a => a.DurationMs);
        var totalDuration = TimeSpan.FromSeconds(assessment.TotalDurationSeconds ?? 0);

        var reliabilityInput = ReliabilityInputBuilder.Build(testBlocks, reliabilityAnswers, reliabilityDurations, totalDuration);
        var reliabilityResult = ReliabilityCalculator.Calculate(reliabilityInput);
        assessment.SetReliability(reliabilityResult.Score, reliabilityResult.Flag, now);

        // QA topilmasi (2026-09-02): TO'LIQ payload bo'yicha solishtiriladi — `ResultCode`/
        // `CompositeIndex` o'zgarmasa ham, `ScoringVersion` yangilanib xom/normallashgan ballar,
        // darajalar yoki bayroqlar boshqacha chiqishi mumkin (masalan formulaning ichki
        // og'irligi o'zgarganda yuqori darajadagi natija kodi bir xil qolib, foizlar siljiydi).
        var changed = oldReliabilityScore != assessment.ReliabilityScore
            || oldReliabilityFlag != assessment.ReliabilityFlag
            || testResults.Any(r =>
            {
                var previous = before[r.TestCode];
                return previous.ResultCode != r.ResultCode
                    || previous.RawScoresJson != r.RawScoresJson
                    || previous.NormalizedScoresJson != r.NormalizedScoresJson
                    || previous.LevelsJson != r.LevelsJson
                    || previous.FlagsJson != r.FlagsJson
                    || previous.ScoringVersion != r.ScoringVersion
                    || previous.CompositeIndex != r.CompositeIndex;
            });

        // Audit'ga esa ATAYLAB qisqa snapshot — `ResultCode`/`CompositeIndex` (shaxsiy ma'lumot
        // yo'q, `prompts/15` MAXSUS DIQQAT #6). To'liq ball payloadi audit yozuvida SHART emas —
        // `TestResult` jadvalining o'zi (SaveChanges bilan) to'liq yangilangan holatni saqlaydi.
        var afterSnapshot = testResults.ToDictionary(r => r.TestCode, r => new { r.ResultCode, r.CompositeIndex });
        var beforeSnapshot = before.ToDictionary(kv => kv.Key, kv => new { kv.Value.ResultCode, kv.Value.CompositeIndex });

        _context.Add(AuditLog.Create(
            AuditActions.AssessmentScoresRecalculated,
            now,
            request.AdminUserId,
            entityType: "Assessment",
            entityId: assessment.Id,
            beforeJson: AuditSnapshot.Serialize(new
            {
                AssessmentId = assessment.Id,
                ReliabilityScore = oldReliabilityScore,
                ReliabilityFlag = oldReliabilityFlag?.ToString(),
                Results = beforeSnapshot,
            }),
            afterJson: AuditSnapshot.Serialize(new
            {
                AssessmentId = assessment.Id,
                ReliabilityScore = assessment.ReliabilityScore,
                ReliabilityFlag = assessment.ReliabilityFlag?.ToString(),
                Results = afterSnapshot,
            }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var resultsDto = await StudentProfileMapping.BuildTestResultsAsync(testResults, _context, _executor, cancellationToken).ConfigureAwait(false);
        var dto = new AdminRecalculateScoresResultDto(assessment.Id, resultsDto, assessment.ReliabilityScore, assessment.ReliabilityFlag?.ToString(), changed);

        return Result.Success(dto);
    }

    /// <summary>
    /// `CompleteSessionCommandHandler.ApplyMaturityIndexIfPossible` bilan BIR XIL mantiq
    /// (`Domain/Scoring` — `CompositeScorer` — o'zgartirilmaydi, faqat chaqiriladi) — bu yerda
    /// ENDI QAYTA HISOBLANGAN BIG5/ACTIVITY `TestResult`laridan o'qiydi.
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
            throw new DomainException(updated.Error.Code, updated.Error.Message);
        }

        bigFive.ApplyCompositeIndex(updated.Value.CompositeIndex!.Value, TestResultJson.Serialize(updated.Value.Levels));
    }
}
