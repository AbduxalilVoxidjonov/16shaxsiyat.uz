using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Admin.Students.List;

/// <summary>
/// `docs/07` 3.2-bo'lim. Read-only — `AsNoTracking`.
///
/// **Ishlash (`prompts/14` MAXSUS DIQQAT #2, ADR-11):** asosiy filtrlash/saralash/sahifalash
/// so'rovi FAQAT `students` jadvaliga tegadi — snapshot ustunlardan (`LastPersonalityType`,
/// `LastMaturityIndex`, `LastActivityLevel`, `NeedsAttention`, `LastAssessmentAt`) o'qiladi,
/// `assessments`/`test_results`ga JOIN QILINMAYDI. Saralash HAM doim DB darajasida (`ORDER BY`,
/// `ix_students_last_at`/standart PK indekslardan foydalanadi) — bu koordinator qarori
/// (2026-09-02): "sinov muhiti uchun production xatti-harakati pasaytirilmaydi". Faqat ikkita
/// istisno, IKKALASI HAM natija SAHIFASI hajmida (≤100 o'quvchi), butun jadval bo'ylab EMAS:
/// 1. `schoolName` — sahifadagi `SchoolId`lar bo'yicha bitta batch so'rov (`Schools`, PK indeksi).
/// 2. `status` filtri berilganda — `EXISTS` sub-so'rov (`ix_assessments_status_started` indeksi
///    ishlaydi), va sahifadagi o'quvchilar uchun `lastAssessmentStatus`/`reliabilityFlag`
///    (`Student` snapshotida YO'Q ustunlar — `AdminStudentListItemDto` izohiga qarang) —
///    sahifa hajmidagi o'quvchilarning BARCHA (ko'p emas — odatda 1-3 ta) sessiyasini
///    `ToListAsync` bilan (ORDER BY'siz — bitta o'quvchining 0-1 ta sessiyasi, kichik to'plam,
///    `StartSessionCommandHandler`dagi TODO(P30) bilan bir xil ildiz sabab, lekin U CHEKLANGAN
///    to'plamda — koordinator qarori shu farqni ATAYLAB istisno qildi) o'qib, XOTIRADA eng
///    yangisini tanlaydi.
///
/// **SQLite (faqat sinov muhiti) eslatmasi.** SQLite provayderi `DateTimeOffset` ustunda
/// `ORDER BY`ni UMUMAN tarjima qila olmaydi (`System.NotSupportedException`). Bu FAQAT sinov
/// muhitining cheklovi — PostgreSQL'da (production) `sort=lastAssessmentAt`/`createdAt`
/// (standart — `-lastAssessmentAt`) to'liq ishlaydi va `ix_students_last_at` indeksidan
/// foydalanadi. Shu sabab BU YERDA production kodi SQLite'ga moslashtirilmaydi — aksincha,
/// sinov loyihasida FAQAT shu ikki saralash yo'lini sinovdan o'tkazadigan testlar aniq
/// `Skip` bilan belgilanadi (`AdminStudentsListEndpointTests`/`AdminStudentsSortEndpointTests`
/// izohiga qarang), P30 (Testcontainers, haqiqiy Postgres) da qayta tekshiriladi.
/// </summary>
internal sealed class ListStudentsQueryHandler : IRequestHandler<ListStudentsQuery, Result<PagedResult<AdminStudentListItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public ListStudentsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<PagedResult<AdminStudentListItemDto>>> Handle(ListStudentsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPagingOptions.Normalize(request.Page, request.PageSize);

        var query = _context.AsNoTracking(_context.Students);

        if (request.SchoolId.HasValue)
        {
            query = query.Where(s => s.SchoolId == request.SchoolId.Value);
        }

        if (request.Grade.HasValue)
        {
            query = query.Where(s => s.Grade == request.Grade.Value);
        }

        if (request.NeedsAttention.HasValue)
        {
            query = query.Where(s => s.NeedsAttention == request.NeedsAttention.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.PersonalityType))
        {
            var personalityType = request.PersonalityType.Trim();
            query = query.Where(s => s.LastPersonalityType == personalityType);
        }

        if (!string.IsNullOrWhiteSpace(request.ActivityLevel) && Enum.TryParse<ActivityLevel>(request.ActivityLevel, ignoreCase: true, out var activityLevel))
        {
            query = query.Where(s => s.LastActivityLevel == activityLevel);
        }

        if (request.From.HasValue)
        {
            query = query.Where(s => s.LastAssessmentAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(s => s.LastAssessmentAt <= request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // `docs/05` 2-bo'lim `ix_students_name_trgm` — `ListSchoolsQueryHandler`dagi bilan bir xil sabab.
            var term = request.Search.Trim();
            query = query.Where(s => s.FullName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<AssessmentStatus>(request.Status, ignoreCase: true, out var status))
        {
            // Sahifalash/saralashdan OLDIN qo'llanadi — filtr faol bo'lganda umumiy sonni ham
            // to'g'ri hisoblash uchun (`ix_assessments_status_started(status, started_at desc)`
            // indeksidan foydalanadigan `EXISTS`).
            query = query.Where(s => _context.Assessments.Any(a => a.StudentId == s.Id && a.Status == status));
        }

        var totalCount = await _executor.CountAsync(query, cancellationToken).ConfigureAwait(false);

        // Standart — `-lastAssessmentAt` (kamayish, `ix_students_last_at` indeksiga mos):
        // `AdminSortSpec.Parse` bo'sh `sort`da `descending=false` qaytaradi, shu sabab bu yerda
        // ANIQ standart qiymat beriladi (parametr berilmasa ham eng faol o'quvchilar birinchi).
        var sortOrDefault = string.IsNullOrWhiteSpace(request.Sort) ? "-lastAssessmentAt" : request.Sort;
        var (sortField, descending) = AdminSortSpec.Parse(sortOrDefault, defaultField: "lastAssessmentAt");

        // Saralash DOIM DB darajasida (koordinator qarori, 2026-09-02) — SQLite (faqat sinov
        // muhiti) `DateTimeOffset` `ORDER BY`ni tarjima qila olmasa, mos test `Skip` qilinadi,
        // production yo'li (Postgres, indeks) pasaytirilmaydi.
        var sortedQuery = ApplySort(query, sortField, descending);
        var pageStudents = await _executor.ToListAsync(
            sortedQuery.Skip((page - 1) * pageSize).Take(pageSize),
            cancellationToken).ConfigureAwait(false);

        if (pageStudents.Count == 0)
        {
            return Result.Success(PagedResult<AdminStudentListItemDto>.Create([], page, pageSize, totalCount));
        }

        var schoolIds = pageStudents.Select(s => s.SchoolId).Distinct().ToList();
        var schoolNames = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Schools).Where(s => schoolIds.Contains(s.Id)).Select(s => new { s.Id, s.Name }),
            cancellationToken).ConfigureAwait(false);
        var schoolNameById = schoolNames.ToDictionary(s => s.Id, s => s.Name);

        var studentIds = pageStudents.Select(s => s.Id).ToList();
        var pageAssessments = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => studentIds.Contains(a.StudentId)),
            cancellationToken).ConfigureAwait(false);
        var latestAssessmentByStudentId = pageAssessments
            .GroupBy(a => a.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartedAt).First());

        var items = pageStudents
            .Select(s =>
            {
                latestAssessmentByStudentId.TryGetValue(s.Id, out var latestAssessment);
                return new AdminStudentListItemDto(
                    s.Id,
                    s.FullName,
                    schoolNameById.GetValueOrDefault(s.SchoolId, "?"),
                    s.Grade,
                    s.ClassLetter,
                    s.Phone.Value,
                    latestAssessment?.Status.ToString(),
                    s.LastPersonalityType,
                    s.LastMaturityIndex,
                    s.LastActivityLevel?.ToString(),
                    s.NeedsAttention,
                    latestAssessment?.ReliabilityFlag?.ToString(),
                    s.LastAssessmentAt);
            })
            .ToList();

        return Result.Success(PagedResult<AdminStudentListItemDto>.Create(items, page, pageSize, totalCount));
    }

    /// <summary>
    /// Oq ro'yxat (`prompts/14` MAXSUS DIQQAT #1). Barchasi DB darajasida — sinf izohiga qarang.
    /// </summary>
    private static IQueryable<Student> ApplySort(IQueryable<Student> query, string field, bool descending) => field switch
    {
        "fullName" => descending ? query.OrderByDescending(s => s.FullName) : query.OrderBy(s => s.FullName),
        "grade" => descending ? query.OrderByDescending(s => s.Grade).ThenBy(s => s.FullName) : query.OrderBy(s => s.Grade).ThenBy(s => s.FullName),
        "createdAt" => descending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
        _ => ApplyLastAssessmentAtSort(query, descending),
    };

    /// <summary>
    /// `NULLS LAST` har ikkala yo'nalishda ham (`docs/05` 2, 5-bo'lim: `ix_students_last_at
    /// (last_assessment_at DESC NULLS LAST)`) — Postgres standart xatti-harakati buning aksi
    /// (`DESC`da standart `NULLS FIRST`), shu sabab `LastAssessmentAt == null` bo'yicha ANIQ,
    /// alohida (DB darajasida) tartiblanadi.
    /// </summary>
    private static IQueryable<Student> ApplyLastAssessmentAtSort(IQueryable<Student> query, bool descending)
    {
        var nullsLast = query.OrderBy(s => s.LastAssessmentAt == null);
        return descending ? nullsLast.ThenByDescending(s => s.LastAssessmentAt) : nullsLast.ThenBy(s => s.LastAssessmentAt);
    }
}
