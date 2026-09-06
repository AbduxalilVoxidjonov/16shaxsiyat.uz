using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
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

        // Filtr qurilishi `AdminStudentFilterBuilder`ga chiqarilgan (`prompts/27` MAXSUS DIQQAT #1)
        // — `ExportStudentsQueryHandler` (`Admin/Students/Export`) AYNAN shu metodni chaqiradi,
        // shu bilan ro'yxat va eksport filtri hech qachon bir-biridan ajralib ketmaydi.
        // Ommaviy makonning `Id`si — MANBA filtri uchun ham, javobdagi `source` ustuni uchun
        // ham kerak (filtr berilmaganda ham har qator qaysi oqimdan kelganini ko'rsatadi).
        // BITTA arzon so'rov (`kind` bo'yicha, bazada bitta qator).
        var publicSpaceId = await AdminSourceFilter
            .FindPublicSpaceIdAsync(_context, _executor, cancellationToken)
            .ConfigureAwait(false);

        var query = AdminStudentFilterBuilder.Apply(
            _context,
            _context.AsNoTracking(_context.Students),
            request.SchoolId,
            request.Grade,
            request.Status,
            request.NeedsAttention,
            request.PersonalityType,
            request.ActivityLevel,
            request.From,
            request.To,
            request.Search,
            AdminSourceFilter.Parse(request.Source),
            publicSpaceId);

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

        // Tip KODI (`INTJ`) yonida to'liq NOM (`Loyihachi`) — egasining talabi (2026-09-03).
        // Sahifa hajmida (≤100 o'quvchi) BITTA batch so'rov; kod bo'yicha `TypeCatalog` PK
        // indeksi ishlaydi. Nom katalogdan keladi (`type-catalog.json`, `CLAUDE.md` 6a-band) —
        // topilmasa `null`, ya'ni UI faqat kodni ko'rsatadi.
        var personalityTypeCodes = pageStudents
            .Select(s => s.LastPersonalityType)
            .Where(code => !string.IsNullOrEmpty(code))
            .Distinct()
            .ToList();

        var typeNameByCode = personalityTypeCodes.Count == 0
            ? []
            : (await _executor.ToListAsync(
                _context.AsNoTracking(_context.TypeCatalog)
                    .Where(t => personalityTypeCodes.Contains(t.Code))
                    .Select(t => new { t.Code, t.NameUz }),
                cancellationToken).ConfigureAwait(false))
                .ToDictionary(t => t.Code, t => t.NameUz, StringComparer.Ordinal);

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
                    s.LastPersonalityType is null ? null : typeNameByCode.GetValueOrDefault(s.LastPersonalityType),
                    s.LastMaturityIndex,
                    s.LastActivityLevel?.ToString(),
                    s.NeedsAttention,
                    latestAssessment?.ReliabilityFlag?.ToString(),
                    s.LastAssessmentAt,
                    AdminSourceFilter.SourceOf(s.SchoolId, publicSpaceId));
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
