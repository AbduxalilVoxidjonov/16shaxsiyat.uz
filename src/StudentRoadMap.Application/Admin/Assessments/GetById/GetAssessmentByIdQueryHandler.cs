using MediatR;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.GetById;

/// <summary>
/// `docs/07` 3.3-bo'lim. Read-only — `AsNoTracking`. `StudentProfileMapping`
/// (`Admin.Students`, `internal` — bir xil assembly ichida ochiq) qayta ishlatiladi: bir xil
/// `TestResult`/`AiAnalysis` → JSON o'girish mantig'ini ikki joyda takrorlamaslik uchun
/// (`GetStudentByIdQueryHandler`dagi bilan bir xil).
/// </summary>
internal sealed class GetAssessmentByIdQueryHandler : IRequestHandler<GetAssessmentByIdQuery, Result<AdminLatestAssessmentDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetAssessmentByIdQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<AdminLatestAssessmentDto>> Handle(GetAssessmentByIdQuery request, CancellationToken cancellationToken)
    {
        var assessment = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<AdminLatestAssessmentDto>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        var testResults = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestResults).Where(r => r.AssessmentId == assessment.Id),
            cancellationToken).ConfigureAwait(false);

        var results = await StudentProfileMapping.BuildTestResultsAsync(testResults, _context, _executor, cancellationToken).ConfigureAwait(false);

        var aiAnalyses = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AiAnalyses).Where(a => a.AssessmentId == assessment.Id),
            cancellationToken).ConfigureAwait(false);

        var currentAiAnalysis = aiAnalyses.FirstOrDefault(a => a.IsCurrent);

        var dto = new AdminLatestAssessmentDto(
            assessment.Id,
            results,
            StudentProfileMapping.BuildAiAnalysis(currentAiAnalysis),
            StudentProfileMapping.BuildAiHistory(aiAnalyses));

        return Result.Success(dto);
    }
}
