using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Dashboard.GetStats;

/// <summary>
/// `docs/07` 3.6-bo'lim, `prompts/15` MAXSUS DIQQAT #2/#3/#4. Read-only — `AsNoTracking`.
/// Soft-deleted yozuvlar (`School`/`Student`/`Assessment`) `IAppDbContext` global so'rov
/// filtri orqali AVTOMATIK chiqarib tashlanadi (`Assessment.IsDeleted` va h.k.) — bu yerda
/// qo'shimcha filtr YOZILMAYDI, chunki xom SQL ishlatilmaydi (faqat LINQ).
///
/// **Ishlash (`prompts/15` MAXSUS DIQQAT #2):** har bir ko'rsatkich — BITTA DB darajasidagi
/// agregat so'rov (`COUNT`/`SUM`/`GROUP BY`); sikl ichida so'rov YO'Q. Sikl faqat sahifa
/// hajmidagi (`RecentAssessmentsLimit` = 10) natijani JSON'ga yig'ishda ishlatiladi (xotirada,
/// so'rovsiz). Jami ~14 so'rov (pastdagi metodlarga qarang) — `ListStudentsQueryHandler`dagi
/// "sahifa hajmida batch" tamoyili bilan bir xil ruhda, lekin bu yerda "sahifa" o'rniga har bir
/// alohida METRIKA o'zining aniq `WHERE`/`GROUP BY` so'rovini oladi.
///
/// **Kesh** (`ICacheService`, 60 soniya, `prompts/15` MAXSUS DIQQAT #2): bitta superadmin
/// (`docs/01` MVP qamrovi) — dashboard barcha (ya'ni yagona) admin uchun bir xil, shu sabab
/// kesh kaliti FOYDALANUVCHIGA emas, faqat SO'RALGAN OYNAGA bog'liq.
///
/// ⚠️ QA topilmasi (2026-09-02, bloklovchi, tuzatildi): kesh kaliti `request.From`/`request.To`
/// **XOM** (kelgan) qiymatlaridan quriladi (`CacheKey(DateTimeOffset?, DateTimeOffset?)`),
/// HISOBLANGAN (`from`/`to`, `?? _dateTime.UtcNow` bilan to'ldirilgan) qiymatlardan EMAS.
/// Avvalgi versiya hisoblangan qiymatlardan kalit qurar edi — parametrsiz so'rovda
/// (`GET .../stats`, dashboard'ning ODATIY yuklanishi) `to ??= UtcNow` HAR safar TIK
/// aniqligida boshqacha chiqib, kesh kaliti hech qachon takrorlanmas, ya'ni `TryGet` hech
/// qachon urmas edi (kesh nazariy jihatdan "ishlar", lekin eng ko'p ishlatiladigan — parametrsiz
/// — yo'lda AMALDA HECH QACHON URMASDI). Endi: "parametrsiz so'rov" o'zi BITTA barqaror kesh
/// kaliti (`"null:null"`); aniq `from`/`to` bilan so'ralganda esa o'sha ANIQ qiymatlar kalit
/// qismi (bir xil `from`/`to` bilan qayta so'ralsa — bir xil kalit).
/// </summary>
internal sealed class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, Result<AdminDashboardStatsDto>>
{
    private const int RecentAssessmentsLimit = 10;
    private const int HollandTopLimit = 10;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly ICacheService _cache;

    public GetDashboardStatsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, ICacheService cache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _cache = cache;
    }

    /// <summary>
    /// Kesh kaliti — `request.From`/`request.To`ning XOM (kelgan) qiymatlaridan, `null` ham
    /// kalit qismi sifatida ("null:null" — parametrsiz so'rov uchun BITTA barqaror kalit).
    /// Hisoblangan (`?? UtcNow` bilan to'ldirilgan) qiymatlardan EMAS — sinf izohidagi QA
    /// topilmasiga qarang.
    /// </summary>
    public static string CacheKey(DateTimeOffset? rawFrom, DateTimeOffset? rawTo) =>
        $"admin-dashboard:stats:{(rawFrom.HasValue ? rawFrom.Value.ToString("O") : "null")}:{(rawTo.HasValue ? rawTo.Value.ToString("O") : "null")}";

    public async Task<Result<AdminDashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(request.From, request.To);
        if (_cache.TryGet<AdminDashboardStatsDto>(cacheKey, out var cached))
        {
            return Result.Success(cached);
        }

        var now = _dateTime.UtcNow;
        var to = request.To ?? now;
        var from = request.From ?? to.AddDays(-30);

        var totals = await BuildTotalsAsync(cancellationToken).ConfigureAwait(false);
        var last30Days = await BuildLast30DaysAsync(from, to, cancellationToken).ConfigureAwait(false);
        var personalityDistribution = await BuildPersonalityDistributionAsync(cancellationToken).ConfigureAwait(false);
        var activityDistribution = await BuildActivityDistributionAsync(cancellationToken).ConfigureAwait(false);
        var hollandTop = await BuildHollandTopAsync(cancellationToken).ConfigureAwait(false);
        var recentAssessments = await BuildRecentAssessmentsAsync(cancellationToken).ConfigureAwait(false);

        var dto = new AdminDashboardStatsDto(totals, last30Days, personalityDistribution, activityDistribution, hollandTop, recentAssessments);
        _cache.Set(cacheKey, dto, CacheDuration);

        return Result.Success(dto);
    }

    /// <summary>2 ta DB agregat so'rov: `schools` (`GROUP BY is_active`), `students`
    /// (`GROUP BY needs_attention`) + 2 ta oddiy `COUNT` (`completedAssessments`,
    /// `pendingAnalysis`). Jami 4 so'rov.</summary>
    private async Task<AdminDashboardTotalsDto> BuildTotalsAsync(CancellationToken cancellationToken)
    {
        var schoolGroups = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Schools)
                .GroupBy(s => s.IsActive)
                .Select(g => new { IsActive = g.Key, Count = g.Count() }),
            cancellationToken).ConfigureAwait(false);
        var schoolsTotal = schoolGroups.Sum(g => g.Count);
        var activeSchools = schoolGroups.Where(g => g.IsActive).Sum(g => g.Count);

        var studentGroups = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Students)
                .GroupBy(s => s.NeedsAttention)
                .Select(g => new { g.Key, Count = g.Count() }),
            cancellationToken).ConfigureAwait(false);
        var studentsTotal = studentGroups.Sum(g => g.Count);
        var needsAttention = studentGroups.Where(g => g.Key).Sum(g => g.Count);

        var completedAssessments = await _executor.CountAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.CompletedAt != null),
            cancellationToken).ConfigureAwait(false);

        var pendingAnalysis = await _executor.CountAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.Status == AssessmentStatus.Analyzing),
            cancellationToken).ConfigureAwait(false);

        return new AdminDashboardTotalsDto(schoolsTotal, activeSchools, studentsTotal, completedAssessments, pendingAnalysis, needsAttention);
    }

    /// <summary>
    /// 4 ta DB so'rov: `newStudents` (`COUNT`), `[from,to]` oralig'ida BOSHLANGAN sessiyalar
    /// `GROUP BY (completed_at IS NULL)` (1 so'rovda ham "boshlangan jami", ham "yakunlanmagan"
    /// — `dropOffRate` shundan, `prompts/15` MAXSUS DIQQAT #4), va yakunlangan qism ustida
    /// 2 ta `SUM` (davomiylik, ishonchlilik — `IAsyncQueryExecutor`da faqat `int`/`decimal`
    /// `SumAsync` bor, `ReliabilityScore`/`TotalDurationSeconds` shu sabab `decimal`ga
    /// castlanadi — Npgsql/SQLite ikkalasida ham tarjima qilinadigan oddiy sonli konversiya).
    /// </summary>
    private async Task<AdminDashboardLast30DaysDto> BuildLast30DaysAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var newStudents = await _executor.CountAsync(
            _context.AsNoTracking(_context.Students).Where(s => s.CreatedAt >= from && s.CreatedAt <= to),
            cancellationToken).ConfigureAwait(false);

        var startedInWindow = _context.AsNoTracking(_context.Assessments).Where(a => a.StartedAt >= from && a.StartedAt <= to);

        var startedGroups = await _executor.ToListAsync(
            startedInWindow
                .GroupBy(a => a.CompletedAt == null)
                .Select(g => new { NotCompleted = g.Key, Count = g.Count() }),
            cancellationToken).ConfigureAwait(false);

        var startedTotal = startedGroups.Sum(g => g.Count);
        var notCompletedTotal = startedGroups.Where(g => g.NotCompleted).Sum(g => g.Count);
        var completedTotal = startedTotal - notCompletedTotal;

        // MAXSUS DIQQAT #4: nol bo'linishga ehtiyot — sof funksiya `AdminDashboardMath`da
        // (DB'siz, `AdminDashboardMathTests`da to'g'ridan-to'g'ri sinaladi).
        var dropOffRate = AdminDashboardMath.DropOffRate(startedTotal, notCompletedTotal);

        var completedInWindow = startedInWindow.Where(a => a.CompletedAt != null);

        var durationSum = completedTotal == 0
            ? 0m
            : await _executor.SumAsync(completedInWindow, a => (decimal)(a.TotalDurationSeconds ?? 0), cancellationToken).ConfigureAwait(false);

        var reliabilitySum = completedTotal == 0
            ? 0m
            : await _executor.SumAsync(completedInWindow, a => (decimal)(a.ReliabilityScore ?? 0), cancellationToken).ConfigureAwait(false);

        var avgDurationMinutes = Math.Round(AdminDashboardMath.SafeAverage(durationSum, completedTotal) / 60.0, 1);
        var avgReliability = Math.Round(AdminDashboardMath.SafeAverage(reliabilitySum, completedTotal), 1);

        return new AdminDashboardLast30DaysDto(newStudents, completedTotal, avgDurationMinutes, avgReliability, Math.Round(dropOffRate, 4));
    }

    /// <summary>1 `GROUP BY` so'rov — `students.last_personality_type` (joriy holat, hamma vaqt).</summary>
    private async Task<IReadOnlyList<AdminDashboardPersonalityItemDto>> BuildPersonalityDistributionAsync(CancellationToken cancellationToken)
    {
        var groups = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Students)
                .Where(s => s.LastPersonalityType != null)
                .GroupBy(s => s.LastPersonalityType!)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count),
            cancellationToken).ConfigureAwait(false);

        return groups.Select(g => new AdminDashboardPersonalityItemDto(g.Type, g.Count)).ToList();
    }

    /// <summary>1 `GROUP BY` so'rov — `students.last_activity_level`.</summary>
    private async Task<IReadOnlyList<AdminDashboardActivityItemDto>> BuildActivityDistributionAsync(CancellationToken cancellationToken)
    {
        var groups = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Students)
                .Where(s => s.LastActivityLevel != null)
                .GroupBy(s => s.LastActivityLevel!.Value)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count),
            cancellationToken).ConfigureAwait(false);

        return groups.Select(g => new AdminDashboardActivityItemDto(g.Level.ToString(), g.Count)).ToList();
    }

    /// <summary>1 `GROUP BY` so'rov — `students.last_holland_code`, eng ko'p uchraydigan `HollandTopLimit` ta.</summary>
    private async Task<IReadOnlyList<AdminDashboardHollandItemDto>> BuildHollandTopAsync(CancellationToken cancellationToken)
    {
        var groups = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Students)
                .Where(s => s.LastHollandCode != null)
                .GroupBy(s => s.LastHollandCode!)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(HollandTopLimit),
            cancellationToken).ConfigureAwait(false);

        return groups.Select(g => new AdminDashboardHollandItemDto(g.Code, g.Count)).ToList();
    }

    /// <summary>
    /// 1 asosiy so'rov (`ORDER BY completed_at DESC` — Postgres'da `ix_assessments_*`
    /// indekslaridan foydalanmasa ham `Take(10)` bilan arzon; SQLite testida ham to'liq
    /// sinaladi — `AppDbContext.ApplySqliteDateTimeOffsetConversion`, `prompts/15` 2-bosqich,
    /// 2026-09-02, `AdminDashboardStatsEndpointTests`ga qarang) + 2 ta batch (talaba/maktab
    /// nomi, sahifa hajmida — `ListStudentsQueryHandler` naqshi). Jami 3 so'rov.
    /// </summary>
    private async Task<IReadOnlyList<AdminDashboardRecentAssessmentDto>> BuildRecentAssessmentsAsync(CancellationToken cancellationToken)
    {
        var recent = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Assessments)
                .Where(a => a.CompletedAt != null)
                .OrderByDescending(a => a.CompletedAt)
                .Take(RecentAssessmentsLimit)
                .Select(a => new { a.Id, a.StudentId, a.SchoolId, CompletedAt = a.CompletedAt!.Value, a.Status }),
            cancellationToken).ConfigureAwait(false);

        if (recent.Count == 0)
        {
            return [];
        }

        var studentIds = recent.Select(a => a.StudentId).Distinct().ToList();
        var students = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Students).Where(s => studentIds.Contains(s.Id)).Select(s => new { s.Id, s.FullName }),
            cancellationToken).ConfigureAwait(false);
        var studentNameById = students.ToDictionary(s => s.Id, s => s.FullName);

        var schoolIds = recent.Select(a => a.SchoolId).Distinct().ToList();
        var schools = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Schools).Where(s => schoolIds.Contains(s.Id)).Select(s => new { s.Id, s.Name }),
            cancellationToken).ConfigureAwait(false);
        var schoolNameById = schools.ToDictionary(s => s.Id, s => s.Name);

        return recent
            .Select(a => new AdminDashboardRecentAssessmentDto(
                a.Id,
                studentNameById.GetValueOrDefault(a.StudentId, "?"),
                schoolNameById.GetValueOrDefault(a.SchoolId, "?"),
                a.CompletedAt,
                a.Status.ToString()))
            .ToList();
    }
}
