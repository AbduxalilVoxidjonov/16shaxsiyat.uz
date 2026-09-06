using MediatR;
using StudentRoadMap.Application.Admin.Schools.LinkHealth;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.Schools.List;

/// <summary>
/// `docs/07` 3.1-bo'lim. Read-only — `AsNoTracking`.
///
/// **Statistika ikkinchi (batch) so'rovda hisoblanadi** — asosiy sahifalash/filtrlash/saralash
/// so'rovi FAQAT `schools` jadvaliga tegadi (JOIN yo'q), keyin sahifa natijasidagi (≤100)
/// maktab ID'lari bo'yicha `students` jadvalidan bitta `GROUP BY` bilan hisoblanadi —
/// `CompletedCount`/`LastActivityAt` `Student` SNAPSHOT ustunlaridan (`CompletedAssessmentCount`,
/// `LastAssessmentAt`) olinadi, `Assessments`/`TestResults`ga umuman murojaat qilinmaydi
/// (`prompts/14` MAXSUS DIQQAT #2 ruhi — `docs/04` ADR-11 aslida `students` ro'yxati uchun,
/// lekin bu yerda ham xuddi shu tamoyil qo'llanildi: qimmat agregatsiya faqat sahifa hajmida).
/// </summary>
internal sealed class ListSchoolsQueryHandler : IRequestHandler<ListSchoolsQuery, Result<PagedResult<AdminSchoolListItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IAppSettings _appSettings;

    public ListSchoolsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IAppSettings appSettings)
    {
        _context = context;
        _executor = executor;
        _appSettings = appSettings;
    }

    public async Task<Result<PagedResult<AdminSchoolListItemDto>>> Handle(ListSchoolsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPagingOptions.Normalize(request.Page, request.PageSize);

        // Ommaviy makon bu ro'yxatda UMUMAN yo'q — `AdminSchoolScope` izohida nima uchun
        // global query filtri emas, aniq `Where` tanlangani asoslangan.
        var query = _context.AsNoTracking(_context.Schools).SchoolsOnly();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // `docs/05` 2-bo'lim: `ix_schools_name_trgm` (`gin_trgm_ops`) Postgres'da oddiy
            // `Contains` (→ `LIKE '%...%'`) so'rovlarini tezlashtiradi — alohida `EF.Functions`
            // chaqiruvi shart emas. SQLite'da (sinov muhiti) `Contains` `instr()`ga tarjima
          // qilinadi — ikkalasida ham ishlaydi (`prompts/14` MAXSUS DIQQAT #10 bilan bir xil sabab).
            var term = request.Search.Trim();
            query = query.Where(s => s.Name.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var region = request.Region.Trim();
            query = query.Where(s => s.Region == region);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        }

        var totalCount = await _executor.CountAsync(query, cancellationToken).ConfigureAwait(false);

        var (sortField, descending) = AdminSortSpec.Parse(request.Sort, defaultField: "name");

        // Saralash DOIM DB darajasida (koordinator qarori, 2026-09-02: "sinov muhiti uchun
        // production xatti-harakati pasaytirilmaydi") — `createdAt` uchun ham. SQLite (faqat
        // sinov muhiti) `DateTimeOffset` `ORDER BY`ni tarjima qila olmasa, mos test aniq `Skip`
        // bilan belgilanadi (`AdminSchoolsSortEndpointTests`), Postgres'da (production) bu
        // yo'l to'liq ishlaydi.
        var sortedQuery = ApplySort(query, sortField, descending);
        var pageSchools = await _executor.ToListAsync(
            sortedQuery.Skip((page - 1) * pageSize).Take(pageSize),
            cancellationToken).ConfigureAwait(false);

        var schoolIds = pageSchools.Select(s => s.Id).ToList();

        // Xom qatorlar (agregatsiyasiz) o'qiladi, so'ng XOTIRADA guruhlanadi — `GROUP BY ... MAX
        // (last_assessment_at)`ni SQLite provayderi (sinov muhiti) DB darajasida tarjima
        // qila olmaydi (`System.NotSupportedException: SQLite cannot apply aggregate operator
        // 'Max' on expressions of type 'DateTimeOffset'`) — `StartSessionCommandHandler`dagi
        // TODO(P30) bilan bir xil ildiz sabab (DateTimeOffset + SQLite). Sahifa hajmidagi (≤100
        // maktab) o'quvchilar bo'yicha ekanligi uchun xavfsiz.
        var studentRows = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Students)
                .Where(s => schoolIds.Contains(s.SchoolId))
                .Select(s => new { s.SchoolId, s.CompletedAssessmentCount, s.LastAssessmentAt }),
            cancellationToken).ConfigureAwait(false);

        var aggregatesBySchoolId = studentRows
            .GroupBy(s => s.SchoolId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    StudentCount = g.Count(),
                    CompletedCount = g.Count(x => x.CompletedAssessmentCount > 0),
                    LastActivityAt = g.Max(x => x.LastAssessmentAt),
                });

        // Havola sog'ligi — sahifadagi maktablar uchun BITTA batch hisob (`SchoolLinkHealthEvaluator`:
        // katalog uchun 4 so'rov + biriktirmalar uchun 1 so'rov, maktab bo'yicha SIKL YO'Q —
        // `completed_assessment_count` naqshi bilan bir xil ruh). Mezon ommaviy
        // `GetSchoolInfoQueryHandler` niki bilan AYNAN bir xil (`ProgramAvailability`) —
        // aks holda panel "hammasi joyida" deb yolg'on aytardi (2026-09-03 jonli hodisasi).
        var linkHealthBySchoolId = await SchoolLinkHealthEvaluator
            .EvaluateManyAsync(_context, _executor, schoolIds, cancellationToken)
            .ConfigureAwait(false);

        var items = pageSchools
            .Select(s =>
            {
                aggregatesBySchoolId.TryGetValue(s.Id, out var aggregate);
                return new AdminSchoolListItemDto(
                    s.Id,
                    s.Name,
                    s.Region,
                    s.District,
                    s.Slug.Value,
                    SchoolMapping.BuildPublicUrl(s, _appSettings),
                    s.IsActive,
                    aggregate?.StudentCount ?? 0,
                    aggregate?.CompletedCount ?? 0,
                    aggregate?.LastActivityAt,
                    linkHealthBySchoolId[s.Id]);
            })
            .ToList();

        return Result.Success(PagedResult<AdminSchoolListItemDto>.Create(items, page, pageSize, totalCount));
    }

    /// <summary>
    /// Oq ro'yxat (`prompts/14` MAXSUS DIQQAT #1) — foydalanuvchi matni to'g'ridan-to'g'ri
    /// `OrderBy`ga uzatilmaydi. Noma'lum maydon jimgina `name`ga tushadi. Barchasi DB darajasida.
    /// </summary>
    private static IQueryable<School> ApplySort(IQueryable<School> query, string field, bool descending) => field switch
    {
        "region" => descending ? query.OrderByDescending(s => s.Region).ThenBy(s => s.Name) : query.OrderBy(s => s.Region).ThenBy(s => s.Name),
        "district" => descending ? query.OrderByDescending(s => s.District).ThenBy(s => s.Name) : query.OrderBy(s => s.District).ThenBy(s => s.Name),
        "createdAt" => descending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
        _ => descending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
    };
}
