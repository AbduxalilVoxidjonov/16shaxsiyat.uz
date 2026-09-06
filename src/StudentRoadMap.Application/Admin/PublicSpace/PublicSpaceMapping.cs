using StudentRoadMap.Application.Admin.Schools.LinkHealth;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.PublicSpace;

/// <summary>
/// `AdminPublicSpaceDto` ni qurish uchun YAGONA joy — `GetPublicSpaceQueryHandler` va uchta
/// mutatsiya handleri (dastur biriktirish/olib tashlash, natija bayrog'i) bir xil javob
/// qaytaradi, ya'ni UI mutatsiyadan keyin qayta so'rov yuborishi shart emas
/// (`SchoolMapping` bilan bir xil naqsh).
///
/// <para>
/// **N+1 YO'Q:** butun DTO uchun katalog snapshoti (`SchoolLinkHealthEvaluator.LoadCatalogAsync`
/// — 4 so'rov) + biriktirmalar (1) + dastur tarkibi soni (1) + statistika (3) = 9 ta so'rov,
/// dastur yoki foydalanuvchi bo'yicha SIKL ichida so'rov yo'q.
/// </para>
/// </summary>
internal static class PublicSpaceMapping
{
    /// <summary>
    /// Ommaviy makon yozuvini TUR bo'yicha topadi (bazada AYNAN BITTA —
    /// `ux_schools_public_space` qisman unikal indeks). `DbSeeder.PublicSpaceSchoolId`
    /// konstantasi `Infrastructure` qatlamida va `Application` unga bog'lana olmaydi
    /// (`docs/06` 3-bo'lim) — `StartPublicSessionCommandHandler` dagi bilan bir xil sabab.
    ///
    /// <para>
    /// **Kuzatiladigan (tracked) so'rov** — chaqiruvchi mutatsiya handleri bo'lishi mumkin
    /// (`SetShowResultToStudent`). O'qish uchun `AsNoTracking` alohida berilmaydi: bitta
    /// qatorli so'rovda farq sezilmaydi, ikki xil yo'l esa "qaysi biri saqlanadi?" degan
    /// tuzoqni yaratardi.
    /// </para>
    /// </summary>
    public static async Task<School?> FindAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        CancellationToken cancellationToken) =>
        await executor.FirstOrDefaultAsync(
            context.Schools.Where(s => s.Kind == SchoolKind.PublicSpace),
            cancellationToken).ConfigureAwait(false);

    /// <summary>Makon topilmaganda qaytariladigan yagona xato (`409 PUBLIC_SPACE_NOT_CONFIGURED`).</summary>
    public static Error NotConfigured() => new(
        ProblemCodes.PublicSpaceNotConfigured,
        "Ommaviy makon sozlanmagan — seed bajarilmagan bo'lishi mumkin.");

    public static async Task<AdminPublicSpaceDto> BuildAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IAppSettings appSettings,
        School space,
        CancellationToken cancellationToken)
    {
        var snapshot = await SchoolLinkHealthEvaluator
            .LoadCatalogAsync(context, executor, cancellationToken)
            .ConfigureAwait(false);

        var assignments = await SchoolLinkHealthEvaluator
            .LoadAssignmentsAsync(context, executor, [space.Id], cancellationToken)
            .ConfigureAwait(false);

        var assignedProgramIds = assignments.GetValueOrDefault(space.Id) ?? new HashSet<Guid>();

        // Mavjudlik mezoni maktablarniki bilan BITTA manbadan — ikkisi ajralib ketsa, panel
        // "hammasi joyida" deb yolg'on aytardi (2026-09-03 jonli hodisasi).
        var linkHealth = SchoolLinkHealthEvaluator.Evaluate(snapshot, assignedProgramIds);

        var programs = await BuildProgramsAsync(
            context, executor, snapshot, assignedProgramIds, cancellationToken).ConfigureAwait(false);

        var stats = await BuildStatsAsync(context, executor, space.Id, cancellationToken).ConfigureAwait(false);

        return new AdminPublicSpaceDto(
            space.Id,
            space.Name,
            space.Slug.Value,
            space.IsActive,
            space.ShowResultToStudent,
            space.DailyRegistrationLimit,
            BuildPublicUrl(appSettings),
            new AdminPublicSpaceAvailabilityDto(
                linkHealth.Status,
                linkHealth.AvailableProgramCount,
                linkHealth.UsableProgramCount),
            programs,
            stats);
    }

    /// <summary>
    /// Ommaviy kirish havolasi — `{PublicWebBaseUrl}/kirish` (`ROUTES.account.login`).
    /// Maktabdagi `/t/{slug}?k={accessToken}` shakli ATAYLAB ishlatilmaydi:
    /// `AdminPublicSpaceDto.PublicUrl` izohiga qarang.
    /// </summary>
    public static string BuildPublicUrl(IAppSettings appSettings) => $"{appSettings.PublicWebBaseUrl}/kirish";

    private static async Task<IReadOnlyList<AdminPublicSpaceProgramDto>> BuildProgramsAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        ProgramCatalogSnapshot snapshot,
        IReadOnlySet<Guid> assignedProgramIds,
        CancellationToken cancellationToken)
    {
        if (assignedProgramIds.Count == 0)
        {
            return [];
        }

        var assignedIds = assignedProgramIds.ToList();

        // Tarkib SONI — BITTA so'rov (`GROUP BY program_id`), dastur bo'yicha sikl yo'q.
        var testCountRows = await executor.ToListAsync(
            context.AsNoTracking(context.ProgramTests)
                .Where(pt => assignedIds.Contains(pt.ProgramId))
                .GroupBy(pt => pt.ProgramId)
                .Select(g => new { ProgramId = g.Key, Count = g.Count() }),
            cancellationToken).ConfigureAwait(false);

        var testCountByProgramId = testCountRows.ToDictionary(r => r.ProgramId, r => r.Count);

        // Katalog allaqachon xotirada (`snapshot.Programs`) — qayta so'rov yuborilmaydi.
        return snapshot.Programs
            .Where(p => assignedProgramIds.Contains(p.Id))
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.NameUz, StringComparer.Ordinal)
            .Select(p => new AdminPublicSpaceProgramDto(
                p.Id,
                p.Code,
                p.NameUz,
                p.State.ToString(),
                p.Visibility.ToString(),
                testCountByProgramId.GetValueOrDefault(p.Id),
                snapshot.ProgramIdsWithUsableTest.Contains(p.Id)))
            .ToList();
    }

    /// <summary>
    /// 3 ta so'rov: foydalanuvchilar (`COUNT`), sessiyalar holat bo'yicha (`GROUP BY status`
    /// — bitta so'rovda jami/jarayonda/tahlil qilingan) va yakunlanganlar (`COUNT`,
    /// `completed_at IS NOT NULL` — `Status` bilan bir xil emas: `Analyzed` ham yakunlangan,
    /// lekin `Abandoned` yakunlanmagan) + eng so'nggi harakat (`ORDER BY ... LIMIT 1`).
    ///
    /// `MAX(updated_at)` o'rniga `ORDER BY ... Take(1)`: `IAsyncQueryExecutor` da `MaxAsync`
    /// yo'q, `ORDER BY` esa ikkala provayderda ham DB darajasida ishlaydi
    /// (`AppDbContext.ApplySqliteDateTimeOffsetConversion` — `DateTimeOffset` SQLite'da `long`).
    /// Butun jadval xotiraga O'QILMAYDI.
    /// </summary>
    private static async Task<AdminPublicSpaceStatsDto> BuildStatsAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        var userCount = await executor.CountAsync(
            context.AsNoTracking(context.Students).Where(s => s.SchoolId == spaceId),
            cancellationToken).ConfigureAwait(false);

        var spaceAssessments = context.AsNoTracking(context.Assessments).Where(a => a.SchoolId == spaceId);

        var statusGroups = await executor.ToListAsync(
            spaceAssessments
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() }),
            cancellationToken).ConfigureAwait(false);

        var totalAssessments = statusGroups.Sum(g => g.Count);
        var inProgressCount = statusGroups.Where(g => g.Status == AssessmentStatus.InProgress).Sum(g => g.Count);
        var analyzedCount = statusGroups.Where(g => g.Status == AssessmentStatus.Analyzed).Sum(g => g.Count);

        var completedCount = await executor.CountAsync(
            spaceAssessments.Where(a => a.CompletedAt != null),
            cancellationToken).ConfigureAwait(false);

        var lastActivity = await executor.ToListAsync(
            spaceAssessments
                .OrderByDescending(a => a.UpdatedAt)
                .Select(a => a.UpdatedAt)
                .Take(1),
            cancellationToken).ConfigureAwait(false);

        return new AdminPublicSpaceStatsDto(
            userCount,
            totalAssessments,
            inProgressCount,
            completedCount,
            analyzedCount,
            lastActivity.Count == 0 ? null : lastActivity[0]);
    }
}
