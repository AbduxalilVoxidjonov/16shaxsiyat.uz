using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Ai.RerunAnalysis;

/// <summary>
/// `docs/07` §3.3, `prompts/18` vazifa #4. `Assessment.MarkAnalyzing` domen qo'riqchisining o'zi
/// ruxsat etilgan boshlang'ich holatlarni (`Completed`/`Analyzed`/`AnalysisFailed`) tekshiradi —
/// noto'g'ri holatdan (masalan `Draft`/`InProgress`) chaqirilsa `DomainException` (`409`) otiladi.
///
/// <para>
/// **Server-tomoni qo'riqchisi (code-review, 2026-09-14):** `MarkAnalyzing`dan OLDIN sessiyada
/// kamida bitta ballanadigan shaxsiyat batareyasi bloki bormi tekshiriladi
/// (`Domain.Catalog.PersonalityBattery.Includes`, `GetAssessmentByIdQueryHandler`/
/// `GetStudentByIdQueryHandler`dagi `HasPersonalityBattery` hisobi bilan BIR XIL manba —
/// test kodi ro'yxati EMAS). So'rovnoma-only sessiyada `CompleteSessionCommandHandler` `Survey`
/// bloklarini tahlildan chiqarib tashlaydi, ya'ni bunday sessiyada qayta tahlil `AnalysisOrchestrator`
/// ni NOL `TestResult` bilan ishga tushirib, bekor va pullik AI so'rovidan so'ng
/// `MarkAnalysisFailed`ga olib boradi. Bayroq YO'Q bo'lsa holat/navbat o'zgarmasdan `409
/// ASSESSMENT_NO_PERSONALITY_BATTERY` qaytariladi.
/// </para>
/// </summary>
internal sealed class RerunAnalysisCommandHandler : IRequestHandler<RerunAnalysisCommand, Result<RerunAnalysisResultDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly IBackgroundJobQueue _backgroundJobQueue;
    private readonly IPostCommitActions _postCommitActions;

    public RerunAnalysisCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IIpHasher ipHasher,
        IBackgroundJobQueue backgroundJobQueue,
        IPostCommitActions postCommitActions)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _backgroundJobQueue = backgroundJobQueue;
        _postCommitActions = postCommitActions;
    }

    public async Task<Result<RerunAnalysisResultDto>> Handle(RerunAnalysisCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var assessment = await _executor.FirstOrDefaultAsync(
            _context.Assessments.Where(a => a.Id == request.AssessmentId),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<RerunAnalysisResultDto>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        // Batareya bo'lmasa (so'rovnoma-only sessiya) — navbatga qo'yishdan OLDIN to'xtatiladi,
        // holat/navbat HECH NARSA o'zgarmaydi (`MarkAnalyzing`dan OLDIN, tranzaksiyaga hech narsa
        // qo'shilmagan holatda).
        var testKindsAndScoringModes = await _executor.ToListAsync(
            from t in _context.AsNoTracking(_context.AssessmentTests)
            where t.AssessmentId == assessment.Id
            join d in _context.AsNoTracking(_context.TestDefinitions) on t.TestDefinitionId equals d.Id
            select new { d.Kind, d.ScoringMode },
            cancellationToken).ConfigureAwait(false);

        // `PersonalityBattery.Includes` DOMEN qoidasidan xotirada chaqiriladi (EF ifodasi sifatida
        // emas) — `GetAssessmentByIdQueryHandler`/`GetStudentByIdQueryHandler`dagi bilan bir xil naqsh.
        var hasPersonalityBattery = testKindsAndScoringModes.Any(t => PersonalityBattery.Includes(t.Kind, t.ScoringMode));

        if (!hasPersonalityBattery)
        {
            return Result.Failure<RerunAnalysisResultDto>(new Error(
                ProblemCodes.AssessmentNoPersonalityBattery,
                "Sessiyada ballanadigan shaxsiyat metodikasi yo'q — AI tahlili mumkin emas."));
        }

        // `Completed`/`Analyzed`/`AnalysisFailed` dan ruxsat etiladi — boshqa holatdan
        // `ASSESSMENT_INVALID_TRANSITION` (`409`) bilan to'xtaydi (domen qo'riqchisi).
        assessment.MarkAnalyzing(now);

        _context.Add(AuditLog.Create(
            AuditActions.AssessmentAnalysisRerun,
            now,
            request.AdminUserId,
            entityType: "Assessment",
            entityId: assessment.Id,
            afterJson: AuditSnapshot.Serialize(new
            {
                AssessmentId = assessment.Id,
                Provider = request.Provider?.ToString(),
                request.PromptVersion,
            }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // P18-R1 bilan bir xil naqsh: navbatga qo'yish tranzaksiya commit bo'lgandan keyin.
        var assessmentId = assessment.Id;
        var provider = request.Provider;
        var promptVersion = request.PromptVersion;
        _postCommitActions.Enqueue(ct => _backgroundJobQueue.EnqueueAiAnalysisAsync(assessmentId, provider, promptVersion, ct));

        return Result.Success(new RerunAnalysisResultDto(assessment.Id, assessment.Status.ToString()));
    }
}
