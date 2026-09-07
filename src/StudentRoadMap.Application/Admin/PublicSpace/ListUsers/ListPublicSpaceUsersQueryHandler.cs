using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.GetSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Admin.PublicSpace.ListUsers;

/// <summary>
/// Read-only (`AsNoTracking`). Manba — `public_users` + `students` (LEFT JOIN,
/// `ux_students_public_user`: bitta akkaunt → 0..1 profil).
///
/// <para>
/// **N+1 YO'Q — jami 5 so'rov, sahifa hajmiga bog'liq emas:**
/// <list type="number">
///   <item>`COUNT` — filtrlangan foydalanuvchilar (DB);</item>
///   <item>sahifa — `ORDER BY … OFFSET/LIMIT` (DB), qidiruv/holat filtri ham DB'da
///   (`in_progress`/`completed` — oxirgi sessiya bo'yicha korrelyatsiyalangan `ORDER BY
///   started_at DESC LIMIT 1` sub-so'rov);</item>
///   <item>sahifadagi o'quvchilarning BARCHA sessiyalari (`student_id IN (…)` — odatda
///   1-3 ta/odam) — jami/yakunlangan/jarayonda va OXIRGI sessiya xotirada;</item>
///   <item>yakunlanmagan oxirgi sessiyalarning test bloklari (`assessment_id IN (…)`);</item>
///   <item>o'sha bloklarning anketa ta'riflari (kod/nom, `id IN (…)`).</item>
/// </list>
/// 4–5 faqat sahifada yakunlanmagan sessiya bo'lsa yuboriladi.
/// </para>
/// <para>
/// "Qayerda to'xtagan" — `SessionProgressCalculator` (ommaviy `GET /api/public/sessions/me`
/// bilan BIR XIL qoida): `DisplayOrder` bo'yicha birinchi `Completed` bo'lmagan blok joriy,
/// uning `AnsweredCount/TotalCount` — "17/44 savol".
/// </para>
/// </summary>
internal sealed class ListPublicSpaceUsersQueryHandler
    : IRequestHandler<ListPublicSpaceUsersQuery, Result<PagedResult<AdminPublicUserListItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;

    public ListPublicSpaceUsersQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
    }

    /// <summary>
    /// LEFT JOIN qatori — `Student` anketa to'ldirilmaguncha `null`. Member-init proyeksiya
    /// (`new UserRow { … }`), konstruktorli record EMAS: EF Core keyingi `Where`/`OrderBy`
    /// dagi `r.User.X` murojaatini faqat a'zo-initsializatsiya orqali ishonchli qayta
    /// bog'laydi.
    /// </summary>
    private sealed class UserRow
    {
        public required PublicUser User { get; init; }

        public Student? Student { get; init; }
    }

    public async Task<Result<PagedResult<AdminPublicUserListItemDto>>> Handle(
        ListPublicSpaceUsersQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPagingOptions.Normalize(request.Page, request.PageSize);
        var status = PublicUserStatusFilter.Parse(request.Status) ?? PublicUserStatusFilter.All;

        var users = _context.AsNoTracking(_context.PublicUsers);
        var students = _context.AsNoTracking(_context.Students);
        var assessments = _context.AsNoTracking(_context.Assessments);

        var rows =
            from u in users
            join s in students on u.Id equals s.PublicUserId into joined
            from s in joined.DefaultIfEmpty()
            select new UserRow { User = u, Student = s };

        rows = ApplySearch(rows, request.Search);
        rows = ApplyStatus(rows, assessments, status);

        var totalCount = await _executor.CountAsync(rows, cancellationToken).ConfigureAwait(false);

        var sortOrDefault = string.IsNullOrWhiteSpace(request.Sort) ? "-registeredAt" : request.Sort;
        var (sortField, descending) = AdminSortSpec.Parse(sortOrDefault, defaultField: "registeredAt");

        var pageRows = await _executor.ToListAsync(
            ApplySort(rows, sortField, descending).Skip((page - 1) * pageSize).Take(pageSize),
            cancellationToken).ConfigureAwait(false);

        if (pageRows.Count == 0)
        {
            return Result.Success(PagedResult<AdminPublicUserListItemDto>.Create([], page, pageSize, totalCount));
        }

        // (3) Sahifadagi o'quvchilarning barcha sessiyalari — BITTA so'rov.
        var studentIds = pageRows.Where(r => r.Student is not null).Select(r => r.Student!.Id).Distinct().ToList();
        var pageAssessments = studentIds.Count == 0
            ? []
            : await _executor.ToListAsync(
                assessments.Where(a => studentIds.Contains(a.StudentId)),
                cancellationToken).ConfigureAwait(false);

        var assessmentsByStudentId = pageAssessments
            .GroupBy(a => a.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartedAt).ThenByDescending(a => a.CreatedAt).ToList());

        // (4)–(5) Yakunlanmagan OXIRGI sessiyalar uchun bloklar va anketa nomlari.
        var openLatestIds = assessmentsByStudentId.Values
            .Select(list => list[0])
            .Where(a => a.CompletedAt is null)
            .Select(a => a.Id)
            .ToList();

        var testsByAssessmentId = new Dictionary<Guid, List<AssessmentTest>>();
        var definitionById = new Dictionary<Guid, (string Code, string NameUz)>();

        if (openLatestIds.Count > 0)
        {
            var openTests = await _executor.ToListAsync(
                _context.AsNoTracking(_context.AssessmentTests).Where(t => openLatestIds.Contains(t.AssessmentId)),
                cancellationToken).ConfigureAwait(false);

            testsByAssessmentId = openTests
                .GroupBy(t => t.AssessmentId)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.DisplayOrder).ToList());

            var definitionIds = openTests.Select(t => t.TestDefinitionId).Distinct().ToList();
            var definitions = await _executor.ToListAsync(
                _context.AsNoTracking(_context.TestDefinitions)
                    .Where(t => definitionIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.Code, t.NameUz }),
                cancellationToken).ConfigureAwait(false);

            definitionById = definitions.ToDictionary(d => d.Id, d => (d.Code, d.NameUz));
        }

        var now = _dateTime.UtcNow;
        var items = pageRows
            .Select(row => MapRow(row, assessmentsByStudentId, testsByAssessmentId, definitionById, now))
            .ToList();

        return Result.Success(PagedResult<AdminPublicUserListItemDto>.Create(items, page, pageSize, totalCount));
    }

    /// <summary>
    /// Qidiruv katta-kichik harf farqsiz (`UPPER(...) LIKE`) — Postgres `LIKE` registrga
    /// sezgir, `ix_students_name_trgm` esa F.I.Sh. uchun; Telegram maydonlarida indeks yo'q
    /// (jadval kichik — o'nlab-minglab akkaunt).
    /// </summary>
    private static IQueryable<UserRow> ApplySearch(IQueryable<UserRow> rows, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return rows;
        }

        var term = search.Trim().ToUpperInvariant();
        var normalizedName = NameNormalizer.Normalize(search);

        return rows.Where(r =>
            (r.User.FirstName != null && r.User.FirstName.ToUpper().Contains(term))
            || (r.User.LastName != null && r.User.LastName.ToUpper().Contains(term))
            || (r.User.Username != null && r.User.Username.ToUpper().Contains(term))
            || (r.Student != null && r.Student.NormalizedName.Contains(normalizedName)));
    }

    /// <summary>
    /// Holat filtri DB darajasida — oxirgi sessiya korrelyatsiyalangan sub-so'rov
    /// (`ORDER BY started_at DESC LIMIT 1`). `never_started` — anketa yo'q YOKI sessiya yo'q.
    /// </summary>
    private static IQueryable<UserRow> ApplyStatus(IQueryable<UserRow> rows, IQueryable<Assessment> assessments, string status) =>
        status switch
        {
            PublicUserStatusFilter.NeverStarted => rows.Where(r =>
                r.Student == null || !assessments.Any(a => a.StudentId == r.Student.Id)),
            PublicUserStatusFilter.InProgress => rows.Where(r =>
                r.Student != null && assessments
                    .Where(a => a.StudentId == r.Student.Id)
                    .OrderByDescending(a => a.StartedAt)
                    .Select(a => a.CompletedAt == null)
                    .FirstOrDefault()),
            PublicUserStatusFilter.Completed => rows.Where(r =>
                r.Student != null && assessments
                    .Where(a => a.StudentId == r.Student.Id)
                    .OrderByDescending(a => a.StartedAt)
                    .Select(a => a.CompletedAt != null)
                    .FirstOrDefault()),
            _ => rows,
        };

    /// <summary>Oq ro'yxat (`prompts/14` MAXSUS DIQQAT #1); noma'lum maydon → `registeredAt`.</summary>
    private static IQueryable<UserRow> ApplySort(IQueryable<UserRow> rows, string field, bool descending) => field switch
    {
        "lastLoginAt" => descending
            ? rows.OrderByDescending(r => r.User.LastLoginAt).ThenBy(r => r.User.Id)
            : rows.OrderBy(r => r.User.LastLoginAt).ThenBy(r => r.User.Id),
        _ => descending
            ? rows.OrderByDescending(r => r.User.CreatedAt).ThenBy(r => r.User.Id)
            : rows.OrderBy(r => r.User.CreatedAt).ThenBy(r => r.User.Id),
    };

    private static AdminPublicUserListItemDto MapRow(
        UserRow row,
        IReadOnlyDictionary<Guid, List<Assessment>> assessmentsByStudentId,
        IReadOnlyDictionary<Guid, List<AssessmentTest>> testsByAssessmentId,
        IReadOnlyDictionary<Guid, (string Code, string NameUz)> definitionById,
        DateTimeOffset now)
    {
        var user = row.User;
        var student = row.Student;

        List<Assessment> studentAssessments = [];
        if (student is not null && assessmentsByStudentId.TryGetValue(student.Id, out var found))
        {
            studentAssessments = found;
        }

        var counts = new AdminPublicUserAssessmentCountsDto(
            studentAssessments.Count,
            studentAssessments.Count(a => a.CompletedAt is not null),
            studentAssessments.Count(a => a.Status is AssessmentStatus.Draft or AssessmentStatus.InProgress));

        AdminPublicUserLastAssessmentDto? last = null;
        if (studentAssessments.Count > 0)
        {
            var latest = studentAssessments[0];
            AdminPublicUserProgressDto? progress = null;

            if (latest.CompletedAt is null)
            {
                testsByAssessmentId.TryGetValue(latest.Id, out var tests);
                progress = BuildProgress(tests ?? [], definitionById);
            }

            last = new AdminPublicUserLastAssessmentDto(
                latest.Id,
                latest.Status.ToString(),
                latest.StartedAt,
                latest.CompletedAt,
                progress);
        }

        return new AdminPublicUserListItemDto(
            user.Id,
            new AdminPublicUserTelegramDto(user.FirstName, user.LastName, user.Username),
            user.CreatedAt,
            user.LastLoginAt,
            student?.Id,
            student?.FullName,
            student is null ? null : AgeCalculator.CalculateAge(student.BirthDate, now),
            student is null || student.Grade == Student.NoGrade ? null : student.Grade,
            counts,
            last);
    }

    /// <summary>Joriy blok — `SessionProgressCalculator.FindCurrentIndex` (ommaviy oqim bilan bir xil).</summary>
    private static AdminPublicUserProgressDto BuildProgress(
        IReadOnlyList<AssessmentTest> orderedTests,
        IReadOnlyDictionary<Guid, (string Code, string NameUz)> definitionById)
    {
        var currentIndex = SessionProgressCalculator.FindCurrentIndex(orderedTests);
        var completed = SessionProgressCalculator.CountCompleted(orderedTests);

        if (currentIndex is null)
        {
            return new AdminPublicUserProgressDto(orderedTests.Count, completed, null, null, null, 0, 0);
        }

        var current = orderedTests[currentIndex.Value];
        definitionById.TryGetValue(current.TestDefinitionId, out var definition);

        return new AdminPublicUserProgressDto(
            orderedTests.Count,
            completed,
            currentIndex.Value + 1,
            definition.Code ?? "?",
            definition.NameUz ?? definition.Code ?? "?",
            current.AnsweredCount,
            current.TotalCount);
    }
}
