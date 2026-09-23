using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;

/// <summary>
/// **Test dasturi** (2026-09-23 egasi qarori, `docs/18` §9.7) — har `TestDefinition` uchun
/// ko'pi bilan BITTA, tizim avtomatik yaratadigan va boshqaradigan `AssessmentProgram`
/// (`OwnerTestDefinitionId`). Admin UI'da "dastur" tushunchasi yo'q — biriktirish (ommaviy /
/// maktablar) test ichida qilinadi, ommaviy oqim (`ProgramAvailability`, `ProgramTestCatalog`,
/// sessiyalar) esa hozirgidek dastur orqali ishlaydi.
///
/// <para>
/// Bu — YAGONA joy, u yerda test dasturi topiladi/yaratiladi va test bilan sinxronlanadi
/// (nom/tavsif/tartib va nashr holati). Chaqiruvchilar: test biriktirish
/// (`UpdateTestAssignmentCommandHandler`), maktab formasi (`SchoolTestAssignment`), ommaviy
/// makon (`AssignPublicSpaceTestCommandHandler`) va test hayotiy sikli handler'lari
/// (yangilash/nashr/faollik/arxiv/o'chirish). `ISender.Send` ishlatilmaydi — sabab
/// `ProgramSchoolAssignment` izohida (ichma-ich tranzaksiya).
/// </para>
/// <para>
/// Hech bir metod `SaveChangesAsync` chaqirmaydi — saqlash chaqiruvchining ishi (bitta
/// tranzaksiya).
/// </para>
/// </summary>
internal static class TestPrograms
{
    /// <summary>Kod band bo'lganda qo'yiladigan prefiks (`T-MBTI16`). Dastur kodi ≤ 30, test kodi ≤ 20.</summary>
    private const string FallbackCodePrefix = "T-";

    /// <summary>
    /// Test dasturini (tracked) topadi yoki `null`. Tarkib (`ProgramTests`) ham ALOHIDA tracked
    /// so'rov bilan yuklanadi — EF Core fixup'i uni `AssessmentProgram.Tests` ga to'ldiradi.
    /// Aks holda `SyncStateWithTest` → `Publish` bo'sh tarkibni ko'rib `PROGRAM_NOT_PUBLISHABLE`
    /// otardi (oldindan biriktirilgan Draft test hech qachon nashr qilinmasdi — code-review
    /// topilmasi, 2026-09-23). `IAppDbContext`da `Include` yo'q — eski
    /// `PublishProgramCommandHandler` ham aynan shu naqshni ishlatgan.
    /// </summary>
    public static async Task<AssessmentProgram?> FindAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid testDefinitionId,
        CancellationToken cancellationToken)
    {
        var program = await executor.FirstOrDefaultAsync(
            context.AssessmentPrograms.Where(p => p.OwnerTestDefinitionId == testDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (program is not null)
        {
            _ = await executor.ToListAsync(
                context.ProgramTests.Where(pt => pt.ProgramId == program.Id),
                cancellationToken).ConfigureAwait(false);
        }

        return program;
    }

    /// <summary>
    /// Test dasturini topadi, bo'lmasa YARATADI (`context.Add`, saqlanmaydi). Har ikkala holda
    /// ham test bilan sinxronlanadi (nom/tavsif/tartib + nashr holati). `Created` — shu chaqiruvda yaratildimi.
    /// </summary>
    public static async Task<(AssessmentProgram Program, bool Created)> EnsureAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        TestDefinition test,
        Guid? adminUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await FindAsync(context, executor, test.Id, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            await SyncAsync(context, executor, existing, test, now, cancellationToken).ConfigureAwait(false);
            return (existing, false);
        }

        var programId = Guid.NewGuid();
        var code = await ResolveCodeAsync(context, executor, test, programId, cancellationToken).ConfigureAwait(false);

        var program = AssessmentProgram.CreateForTest(
            programId,
            test.Id,
            code,
            test.NameUz,
            test.DescriptionUz,
            test.DisplayOrder,
            now,
            adminUserId);

        program.SyncStateWithTest(test.Status, test.IsActive, test.IsPersonalityBattery, now);

        context.Add(program);

        return (program, true);
    }

    /// <summary>
    /// Test o'zgargandan keyin (nom, nashr, faollik, arxiv) test dasturini unga moslaydi.
    /// Test dasturi bo'lmasa hech narsa qilmaydi (yangisini YARATMAYDI).
    /// </summary>
    public static async Task SyncIfExistsAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        TestDefinition test,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var program = await FindAsync(context, executor, test.Id, cancellationToken).ConfigureAwait(false);

        if (program is not null)
        {
            await SyncAsync(context, executor, program, test, now, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Berilgan test dasturi uchun `school_programs` biriktirmalarini
    /// FAQAT maktablar (`SchoolKind.School`) bo'yicha to'liq almashtiradi. Ommaviy makon
    /// biriktirmasi bu yerda TEGILMAYDI (u alohida bayroq — `SetPublicSpaceLinkAsync`).
    /// O'zgarish bo'lsa `true`.
    /// </summary>
    public static async Task<bool> ReplaceSchoolLinksAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid programId,
        IReadOnlyCollection<Guid> schoolIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var currentLinks = await executor.ToListAsync(
            context.SchoolPrograms
                .Where(sp => sp.ProgramId == programId)
                .Join(
                    context.Schools,
                    sp => sp.SchoolId,
                    s => s.Id,
                    (sp, s) => new { Link = sp, s.Kind }),
            cancellationToken).ConfigureAwait(false);

        var target = schoolIds.ToHashSet();
        var changed = false;

        foreach (var row in currentLinks.Where(r => r.Kind == SchoolKind.School))
        {
            if (!target.Remove(row.Link.SchoolId))
            {
                context.Remove(row.Link);
                changed = true;
            }
        }

        foreach (var schoolId in target)
        {
            context.Add(SchoolProgram.Create(Guid.NewGuid(), schoolId, programId, now));
            changed = true;
        }

        return changed;
    }

    /// <summary>Test dasturini ommaviy makonga biriktiradi yoki olib tashlaydi (idempotent). O'zgarish bo'lsa `true`.</summary>
    public static async Task<bool> SetPublicSpaceLinkAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid programId,
        Guid publicSpaceId,
        bool linked,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var link = await executor.FirstOrDefaultAsync(
            context.SchoolPrograms.Where(sp => sp.ProgramId == programId && sp.SchoolId == publicSpaceId),
            cancellationToken).ConfigureAwait(false);

        if (linked && link is null)
        {
            context.Add(SchoolProgram.Create(Guid.NewGuid(), publicSpaceId, programId, now));
            return true;
        }

        if (!linked && link is not null)
        {
            context.Remove(link);
            return true;
        }

        return false;
    }

    private static async Task SyncAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        AssessmentProgram program,
        TestDefinition test,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var code = program.Code == test.Code
            ? program.Code
            : await ResolveCodeAsync(context, executor, test, program.Id, cancellationToken).ConfigureAwait(false);

        program.SyncDetailsFromTest(code, test.NameUz, test.DescriptionUz, test.DisplayOrder, now);
        program.SyncStateWithTest(test.Status, test.IsActive, test.IsPersonalityBattery, now);
    }

    /// <summary>
    /// Dastur kodi — test kodi (ommaviy `programCode` ham shu). Boshqa (eski) dastur shu kodni
    /// band qilgan bo'lsa `T-{kod}`, u ham band bo'lsa `T-{dastur ID}` (30 belgigacha).
    /// </summary>
    private static async Task<string> ResolveCodeAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        TestDefinition test,
        Guid programId,
        CancellationToken cancellationToken)
    {
        string[] candidates =
        [
            test.Code,
            FallbackCodePrefix + test.Code,
            (FallbackCodePrefix + programId.ToString("N"))[..30],
        ];

        foreach (var candidate in candidates)
        {
            var taken = await executor.AnyAsync(
                context.AssessmentPrograms.Where(p => p.Code == candidate && p.Id != programId),
                cancellationToken).ConfigureAwait(false);

            if (!taken)
            {
                return candidate;
            }
        }

        return candidates[^1];
    }
}
