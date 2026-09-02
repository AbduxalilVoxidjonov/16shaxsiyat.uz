using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Students.GetById;

/// <summary>
/// `docs/07` 3.2-bo'lim: `{student, assessments, latestAssessment}`. Read-only — `AsNoTracking`.
/// `latestAssessment.aiAnalysis`/`aiHistory` — P16-P18 (AI modul) ulanmaguncha odatda `null`/bo'sh
/// (`prompts/14` Vazifa 2-band ruxsati).
/// </summary>
internal sealed class GetStudentByIdQueryHandler : IRequestHandler<GetStudentByIdQuery, Result<AdminStudentProfileDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;

    public GetStudentByIdQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
    }

    public async Task<Result<AdminStudentProfileDto>> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        var student = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Students).Where(s => s.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (student is null)
        {
            return Result.Failure<AdminStudentProfileDto>(new Error(ProblemCodes.NotFound, "O'quvchi topilmadi."));
        }

        var school = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Schools).Where(s => s.Id == student.SchoolId),
            cancellationToken).ConfigureAwait(false);

        var now = _dateTime.UtcNow;
        var studentDto = new AdminStudentDetailDto(
            student.Id,
            student.FullName,
            student.BirthDate,
            CalculateAge(student.BirthDate, now),
            student.Gender.ToString(),
            student.Grade,
            student.ClassLetter,
            student.Phone.Value,
            student.ParentPhone?.Value,
            student.Email,
            new AdminStudentSchoolRefDto(student.SchoolId, school?.Name ?? "?"),
            student.ConsentGivenAt,
            student.CreatedAt);

        // Bir nechta sessiya bo'lishi mumkin, lekin odatda 1-2 ta — `ToListAsync` (ORDER BY'siz,
        // SQLite `DateTimeOffset` cheklovi) + xotirada tartiblash, `ListStudentsQueryHandler`dagi
        // bilan bir xil sabab.
        var assessments = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.StudentId == student.Id),
            cancellationToken).ConfigureAwait(false);
        var orderedAssessments = assessments.OrderByDescending(a => a.StartedAt).ToList();

        var assessmentDtos = orderedAssessments
            .Select((a, index) => new AdminAssessmentSummaryDto(
                a.Id,
                a.Status.ToString(),
                a.StartedAt,
                a.CompletedAt,
                a.TotalDurationSeconds.HasValue ? a.TotalDurationSeconds.Value / 60 : null,
                a.ReliabilityScore,
                a.ReliabilityFlag?.ToString(),
                IsLatest: index == 0))
            .ToList();

        var latestAssessment = orderedAssessments.FirstOrDefault();
        AdminLatestAssessmentDto? latestAssessmentDto = null;

        if (latestAssessment is not null)
        {
            var testResults = await _executor.ToListAsync(
                _context.AsNoTracking(_context.TestResults).Where(r => r.AssessmentId == latestAssessment.Id),
                cancellationToken).ConfigureAwait(false);

            var results = await StudentProfileMapping.BuildTestResultsAsync(testResults, _context, _executor, cancellationToken).ConfigureAwait(false);

            var aiAnalyses = await _executor.ToListAsync(
                _context.AsNoTracking(_context.AiAnalyses).Where(a => a.AssessmentId == latestAssessment.Id),
                cancellationToken).ConfigureAwait(false);

            var currentAiAnalysis = aiAnalyses.FirstOrDefault(a => a.IsCurrent);

            latestAssessmentDto = new AdminLatestAssessmentDto(
                latestAssessment.Id,
                results,
                StudentProfileMapping.BuildAiAnalysis(currentAiAnalysis),
                StudentProfileMapping.BuildAiHistory(aiAnalyses));
        }

        var profile = new AdminStudentProfileDto(studentDto, assessmentDtos, latestAssessmentDto);

        return Result.Success(profile);
    }

    private static int CalculateAge(DateOnly birthDate, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
