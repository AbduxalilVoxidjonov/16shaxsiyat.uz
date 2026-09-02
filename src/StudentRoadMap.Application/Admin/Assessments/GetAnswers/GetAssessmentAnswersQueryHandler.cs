using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.GetAnswers;

/// <summary>
/// `docs/07` 3.3-bo'lim, `prompts/15` MAXSUS DIQQAT #5. Read-only — `AsNoTracking`. Audit
/// uchun XOM javoblar: savol matni (`textUz` — `CLAUDE.md` 1-band: admin UI ham o'zbekcha),
/// javob, `durationMs`, `revisionCount`. `scale`/`scaleDirection` QASDAN chiqarilmaydi.
///
/// Bog'langan hajm (≤ ~190 javob/sessiya, `docs/05` §5) — barcha lug'atlar (savollar,
/// variantlar, test kodlari) SAHIFA emas, BUTUN sessiya doirasida, lekin har biri kamida bitta
/// (sikl ichida so'rovsiz) batch so'rov bilan yuklanadi.
/// </summary>
internal sealed class GetAssessmentAnswersQueryHandler : IRequestHandler<GetAssessmentAnswersQuery, Result<IReadOnlyList<AdminAssessmentAnswerDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetAssessmentAnswersQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<IReadOnlyList<AdminAssessmentAnswerDto>>> Handle(GetAssessmentAnswersQuery request, CancellationToken cancellationToken)
    {
        var assessmentExists = await _executor.AnyAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (!assessmentExists)
        {
            return Result.Failure<IReadOnlyList<AdminAssessmentAnswerDto>>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        var assessmentTests = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AssessmentTests)
                .Where(t => t.AssessmentId == request.Id)
                .Select(t => new { t.Id, t.TestDefinitionId, t.DisplayOrder }),
            cancellationToken).ConfigureAwait(false);

        if (assessmentTests.Count == 0)
        {
            return Result.Success<IReadOnlyList<AdminAssessmentAnswerDto>>([]);
        }

        var testDefinitionIds = assessmentTests.Select(t => t.TestDefinitionId).Distinct().ToList();
        var testDefinitions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => testDefinitionIds.Contains(t.Id)).Select(t => new { t.Id, t.Code }),
            cancellationToken).ConfigureAwait(false);
        var testCodeByDefinitionId = testDefinitions.ToDictionary(t => t.Id, t => t.Code);

        var relevantTests = string.IsNullOrWhiteSpace(request.TestCode)
            ? assessmentTests
            : assessmentTests
                .Where(t => testCodeByDefinitionId.TryGetValue(t.TestDefinitionId, out var code)
                    && string.Equals(code, request.TestCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (relevantTests.Count == 0)
        {
            return Result.Success<IReadOnlyList<AdminAssessmentAnswerDto>>([]);
        }

        var testCodeByAssessmentTestId = relevantTests.ToDictionary(
            t => t.Id,
            t => testCodeByDefinitionId.GetValueOrDefault(t.TestDefinitionId, string.Empty));
        var sessionOrderByAssessmentTestId = relevantTests.ToDictionary(t => t.Id, t => t.DisplayOrder);

        var assessmentTestIds = relevantTests.Select(t => t.Id).ToList();
        var answers = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Answers).Where(a => assessmentTestIds.Contains(a.AssessmentTestId)),
            cancellationToken).ConfigureAwait(false);

        if (answers.Count == 0)
        {
            return Result.Success<IReadOnlyList<AdminAssessmentAnswerDto>>([]);
        }

        var questionIds = answers.Select(a => a.QuestionId).Distinct().ToList();
        var questions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions)
                .Where(q => questionIds.Contains(q.Id))
                .Select(q => new { q.Id, q.Code, q.TextUz, q.DisplayOrder }),
            cancellationToken).ConfigureAwait(false);
        var questionById = questions.ToDictionary(q => q.Id);

        var optionIds = answers.Where(a => a.SelectedOptionId.HasValue).Select(a => a.SelectedOptionId!.Value).Distinct().ToList();
        var options = optionIds.Count == 0
            ? []
            : await _executor.ToListAsync(
                _context.AsNoTracking(_context.AnswerOptions).Where(o => optionIds.Contains(o.Id)).Select(o => new { o.Id, o.TextUz }),
                cancellationToken).ConfigureAwait(false);
        var optionTextById = options.ToDictionary(o => o.Id, o => o.TextUz);

        var items = answers
            .Where(a => questionById.ContainsKey(a.QuestionId))
            .Select(a =>
            {
                var question = questionById[a.QuestionId];
                return new
                {
                    Dto = new AdminAssessmentAnswerDto(
                        a.QuestionId,
                        question.Code,
                        testCodeByAssessmentTestId.GetValueOrDefault(a.AssessmentTestId, string.Empty),
                        question.TextUz,
                        a.RawValue,
                        a.SelectedOptionId.HasValue ? optionTextById.GetValueOrDefault(a.SelectedOptionId.Value) : null,
                        a.DurationMs,
                        a.RevisionCount,
                        a.AnsweredAt),
                    SessionOrder = sessionOrderByAssessmentTestId.GetValueOrDefault(a.AssessmentTestId),
                    QuestionOrder = question.DisplayOrder,
                };
            })
            // Audit uchun tushunarli tartib: avval sessiya ICHIDAGI test tartibi, so'ng
            // TEST ICHIDAGI savol tartibi (ikkalasi ham `int` — SQLite `ORDER BY` cheklovi
            // faqat `DateTimeOffset`ga tegishli, bu yerda muammo yo'q).
            .OrderBy(x => x.SessionOrder)
            .ThenBy(x => x.QuestionOrder)
            .Select(x => x.Dto)
            .ToList();

        return Result.Success<IReadOnlyList<AdminAssessmentAnswerDto>>(items);
    }
}
