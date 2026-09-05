using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.ListAssessments;

/// <summary>
/// Foydalanuvchining BARCHA sessiyalari — `students.public_user_id` orqali. Read-only
/// (`AsNoTracking`).
///
/// **Muddati o'tgan sessiyalar ham ko'rsatiladi:** `Assessment.ExpiresAt` — bu SESSIYA
/// TOKENINING (test yechish oynasi, 7 kun) muddati, arxivning emas. Kabinetda egalik JWT
/// bilan isbotlanadi, shu sabab tarix o'chib qolmaydi (`GetMyAssessmentResultQueryHandler`
/// izohi bilan bir xil asos).
/// </summary>
internal sealed class ListMyAssessmentsQueryHandler : IRequestHandler<ListMyAssessmentsQuery, Result<ListMyAssessmentsResult>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IAppSettings _appSettings;

    public ListMyAssessmentsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IAppSettings appSettings)
    {
        _context = context;
        _executor = executor;
        _appSettings = appSettings;
    }

    public async Task<Result<ListMyAssessmentsResult>> Handle(ListMyAssessmentsQuery request, CancellationToken cancellationToken)
    {
        var studentIds = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Students)
                .Where(s => s.PublicUserId == request.PublicUserId)
                .Select(s => s.Id),
            cancellationToken).ConfigureAwait(false);

        if (studentIds.Count == 0)
        {
            return Result.Success(new ListMyAssessmentsResult([]));
        }

        var assessments = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Assessments)
                .Where(a => studentIds.Contains(a.StudentId))
                .Select(a => new AssessmentRow(a.Id, a.SchoolId, a.ProgramId, a.Status, a.StartedAt, a.CompletedAt)),
            cancellationToken).ConfigureAwait(false);

        if (assessments.Count == 0)
        {
            return Result.Success(new ListMyAssessmentsResult([]));
        }

        var programIds = assessments.Select(a => a.ProgramId).Distinct().ToList();
        var programs = (await _executor.ToListAsync(
                _context.AsNoTracking(_context.AssessmentPrograms)
                    .Where(p => programIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.Code, p.NameUz }),
                cancellationToken).ConfigureAwait(false))
            .ToDictionary(p => p.Id);

        var schoolIds = assessments.Select(a => a.SchoolId).Distinct().ToList();
        var schools = (await _executor.ToListAsync(
                _context.AsNoTracking(_context.Schools)
                    .Where(s => schoolIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.ShowResultToStudent }),
                cancellationToken).ConfigureAwait(false))
            .ToDictionary(s => s.Id, s => s.ShowResultToStudent);

        // TODO(P30): `OrderByDescending(DateTimeOffset)` server tomonida ATAYLAB
        // ishlatilmaydi — SQLite provayderi (integratsiya sinovlari muhiti) uni tarjima
        // qila olmaydi (`StartSessionCommandHandler` dagi bir xil izoh/texnik qarz).
        var items = assessments
            .OrderByDescending(a => a.StartedAt)
            .Select(a =>
            {
                var program = programs.GetValueOrDefault(a.ProgramId);
                var spaceAllows = schools.TryGetValue(a.SchoolId, out var showResult) && showResult;

                return new MyAssessmentDto(
                    a.Id,
                    a.Status.ToString(),
                    a.StartedAt,
                    a.CompletedAt,
                    program?.Code ?? "",
                    program?.NameUz ?? "",
                    ResultAvailable: a.Status == AssessmentStatus.Analyzed
                        && ShowResultPolicy.IsAllowed(_appSettings.ShowResultToStudent, spaceAllows));
            })
            .ToList();

        return Result.Success(new ListMyAssessmentsResult(items));
    }

    /// <summary>
    /// EF proyeksiyasi uchun oraliq tur — butun `Assessment` agregatini (javoblar, testlar,
    /// `session_token_hash`) xotiraga tortmaslik uchun.
    /// </summary>
    private sealed record AssessmentRow(
        Guid Id,
        Guid SchoolId,
        Guid ProgramId,
        AssessmentStatus Status,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt);
}
