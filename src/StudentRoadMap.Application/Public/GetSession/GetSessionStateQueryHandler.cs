using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.GetSession;

/// <summary>
/// `docs/07` 1.3-bo'lim. Read-only — `AsNoTracking` (`docs/06` 4-bo'lim konvensiyasi).
///
/// `"Locked"` holati domen `TestStatus` enum'ida yo'q (`Application.Public.Common.PublicTestSummaryDto`
/// izohiga qarang) — proyeksiya darajasida hisoblanadi: `DisplayOrder` bo'yicha birinchi
/// `Completed` bo'lmagan testdan keyingi barcha testlar `"Locked"` ko'rinadi.
/// </summary>
internal sealed class GetSessionStateQueryHandler : IRequestHandler<GetSessionStateQuery, Result<GetSessionStateResult>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;

    public GetSessionStateQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
    }

    public async Task<Result<GetSessionStateResult>> Handle(GetSessionStateQuery request, CancellationToken cancellationToken)
    {
        var assessment = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.Id == request.AssessmentId),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<GetSessionStateResult>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        if (assessment.ExpiresAt <= _dateTime.UtcNow)
        {
            return Result.Failure<GetSessionStateResult>(new Error(ProblemCodes.SessionExpired, "Sessiyaning amal qilish muddati tugagan."));
        }

        var student = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Students).Where(s => s.Id == assessment.StudentId),
            cancellationToken).ConfigureAwait(false);

        if (student is null)
        {
            return Result.Failure<GetSessionStateResult>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        var assessmentTests = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AssessmentTests).Where(t => t.AssessmentId == assessment.Id).OrderBy(t => t.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var testDefinitionIds = assessmentTests.Select(t => t.TestDefinitionId).ToList();
        var definitionLookup = (await _executor.ToListAsync(
                _context.AsNoTracking(_context.TestDefinitions)
                    .Where(t => testDefinitionIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.Code, t.NameUz, t.EstimatedMinutes, t.Kind, t.ScoringMode }),
                cancellationToken).ConfigureAwait(false))
            .ToDictionary(x => x.Id, x => x);

        var tests = new List<PublicTestSummaryDto>(assessmentTests.Count);
        string? currentTestCode = null;
        var locked = false;

        // `docs/06` 8-bo'lim (2026-09-02): dasturda shaxsiyat batareyasi BO'LMASLIGI mumkin.
        // Mezon — `Domain.Catalog.PersonalityBattery` (kod satri emas); ta'rif topilmasa
        // (nazariy: anketa o'chirilgan) batareya YO'Q deb hisoblanadi — "ma'lumot yo'q"ni
        // "bor" bilan almashtirmaymiz.
        var hasPersonalityBattery = false;

        foreach (var test in assessmentTests)
        {
            var definition = definitionLookup.GetValueOrDefault(test.TestDefinitionId);
            var code = definition?.Code ?? "?";
            string status;

            if (definition is not null && PersonalityBattery.Includes(definition.Kind, definition.ScoringMode))
            {
                hasPersonalityBattery = true;
            }

            if (locked)
            {
                status = "Locked";
            }
            else if (test.Status == TestStatus.Completed)
            {
                status = TestStatus.Completed.ToString();
            }
            else
            {
                status = test.Status.ToString();
                currentTestCode ??= code;
                locked = true; // Birinchi tugallanmagan testdan keyingilari qulflangan (`docs/07` 1.3 namunasi).
            }

            tests.Add(new PublicTestSummaryDto(
                code,
                definition?.NameUz ?? code,
                status,
                test.AnsweredCount,
                test.TotalCount,
                test.DisplayOrder,
                definition?.EstimatedMinutes ?? 0));
        }

        var totalAnswered = assessmentTests.Sum(t => t.AnsweredCount);
        var totalQuestions = assessmentTests.Sum(t => t.TotalCount);
        var progressPercent = totalQuestions == 0 ? 0 : (int)Math.Round(100.0 * totalAnswered / totalQuestions, MidpointRounding.AwayFromZero);

        var result = new GetSessionStateResult(
            assessment.Id,
            assessment.Status.ToString(),
            new PublicStudentSummaryDto(StudentNameHelper.ExtractFirstNameShort(student.FullName), student.Grade),
            assessment.ExpiresAt,
            currentTestCode,
            tests,
            progressPercent,
            hasPersonalityBattery);

        return Result.Success(result);
    }
}
