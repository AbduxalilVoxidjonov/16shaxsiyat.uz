using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.List;

/// <summary>
/// `docs/07` 3.3-bo'lim. Read-only — `AsNoTracking`. Soft-deleted sessiyalar `IAppDbContext`
/// global so'rov filtri orqali avtomatik chiqarib tashlanadi (`Assessment.IsDeleted`).
///
/// **Ishlash (`prompts/15` MAXSUS DIQQAT #1, `ListStudentsQueryHandler` naqshi):** filtr/
/// saralash/sahifalash — FAQAT `assessments` jadvaliga, DB darajasida (`schoolId` berilganda
/// `ix_assessments_school_status`, `status` berilganda `ix_assessments_status_started`).
/// Sahifa hajmidagi (≤100) o'quvchi/maktab nomlari — IKKITA batch so'rov (JOIN emas), xuddi
/// `ListStudentsQueryHandler`dagi `schoolName` yechimi kabi.
///
/// **SQLite (faqat sinov muhiti) eslatmasi:** standart saralash (`-startedAt`, `DateTimeOffset`)
/// avval SQLite'da `ORDER BY`ni tarjima qila olmasdi — `AppDbContext.ApplySqliteDateTimeOffsetConversion`
/// (`DateTimeOffset` → `long` UTC tick, faqat SQLite'da) bilan yopildi (`prompts/15` 2-bosqich,
/// 2026-09-02): `AdminAssessmentsSortEndpointTests` endi `Skip`siz, to'liq o'tadi.
/// </summary>
internal sealed class ListAssessmentsQueryHandler : IRequestHandler<ListAssessmentsQuery, Result<PagedResult<AdminAssessmentListItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public ListAssessmentsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<PagedResult<AdminAssessmentListItemDto>>> Handle(ListAssessmentsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPagingOptions.Normalize(request.Page, request.PageSize);

        var query = _context.AsNoTracking(_context.Assessments);

        if (request.SchoolId.HasValue)
        {
            query = query.Where(a => a.SchoolId == request.SchoolId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<AssessmentStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (request.From.HasValue)
        {
            query = query.Where(a => a.StartedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(a => a.StartedAt <= request.To.Value);
        }

        var totalCount = await _executor.CountAsync(query, cancellationToken).ConfigureAwait(false);

        var sortOrDefault = string.IsNullOrWhiteSpace(request.Sort) ? "-startedAt" : request.Sort;
        var (sortField, descending) = AdminSortSpec.Parse(sortOrDefault, defaultField: "startedAt");

        var sortedQuery = ApplySort(query, sortField, descending);
        var pageAssessments = await _executor.ToListAsync(
            sortedQuery.Skip((page - 1) * pageSize).Take(pageSize),
            cancellationToken).ConfigureAwait(false);

        if (pageAssessments.Count == 0)
        {
            return Result.Success(PagedResult<AdminAssessmentListItemDto>.Create([], page, pageSize, totalCount));
        }

        var studentIds = pageAssessments.Select(a => a.StudentId).Distinct().ToList();
        var students = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Students).Where(s => studentIds.Contains(s.Id)).Select(s => new { s.Id, s.FullName }),
            cancellationToken).ConfigureAwait(false);
        var studentNameById = students.ToDictionary(s => s.Id, s => s.FullName);

        var schoolIds = pageAssessments.Select(a => a.SchoolId).Distinct().ToList();
        var schools = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Schools).Where(s => schoolIds.Contains(s.Id)).Select(s => new { s.Id, s.Name }),
            cancellationToken).ConfigureAwait(false);
        var schoolNameById = schools.ToDictionary(s => s.Id, s => s.Name);

        // Dastur nomi (2026-09-03) — o'quvchi/maktab nomi bilan BIR XIL naqsh: sahifadagi
        // (≤100) UNIKAL `programId`lar bo'yicha BITTA batch so'rov, sikl ichida emas.
        var programIds = pageAssessments.Select(a => a.ProgramId).Distinct().ToList();
        var programs = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AssessmentPrograms).Where(p => programIds.Contains(p.Id)).Select(p => new { p.Id, p.NameUz }),
            cancellationToken).ConfigureAwait(false);
        var programNameById = programs.ToDictionary(p => p.Id, p => p.NameUz);

        var items = pageAssessments
            .Select(a => new AdminAssessmentListItemDto(
                a.Id,
                a.StudentId,
                studentNameById.GetValueOrDefault(a.StudentId, "?"),
                a.SchoolId,
                schoolNameById.GetValueOrDefault(a.SchoolId, "?"),
                a.Status.ToString(),
                a.StartedAt,
                a.CompletedAt,
                a.TotalDurationSeconds.HasValue ? a.TotalDurationSeconds.Value / 60 : null,
                a.ReliabilityScore,
                a.ReliabilityFlag?.ToString(),
                a.ProgramId,
                // Dastur yozuvi topilmasa `null` — soxta nom yoki bo'sh satr EMAS.
                programNameById.GetValueOrDefault(a.ProgramId)))
            .ToList();

        return Result.Success(PagedResult<AdminAssessmentListItemDto>.Create(items, page, pageSize, totalCount));
    }

    /// <summary>Oq ro'yxat (`prompts/15` MAXSUS DIQQAT #1) — barchasi DB darajasida.</summary>
    private static IQueryable<Assessment> ApplySort(IQueryable<Assessment> query, string field, bool descending) => field switch
    {
        "completedAt" => descending ? query.OrderByDescending(a => a.CompletedAt) : query.OrderBy(a => a.CompletedAt),
        "reliabilityScore" => descending ? query.OrderByDescending(a => a.ReliabilityScore) : query.OrderBy(a => a.ReliabilityScore),
        _ => descending ? query.OrderByDescending(a => a.StartedAt) : query.OrderBy(a => a.StartedAt),
    };
}
