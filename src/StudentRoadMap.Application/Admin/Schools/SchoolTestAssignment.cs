using StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools;

/// <summary>
/// Maktab formasi tomonidan test biriktirish (2026-09-23 egasi qarori, `docs/18` §9.7):
/// ilgari maktabga DASTUR biriktirilardi, endi TESTLAR tanlanadi — ichkarida har test o'z
/// test dasturiga (`TestPrograms`) map qilinadi, test dasturi bo'lmasa yaratiladi.
///
/// <para>
/// Faqat TEST dasturlari biriktirmalari boshqariladi: eski (ko'p testli / tizim) dasturning
/// shu maktabga biriktirmasi bo'lsa — u TEGILMAYDI (admin UI'da ko'rinmaydi, lekin sessiya
/// tarixi va mavjud havolalar buzilmasin).
/// </para>
/// </summary>
internal static class SchoolTestAssignment
{
    /// <summary>Shu maktabga biriktirilgan test dasturlarining testlari (biriktirilgan tartibda).</summary>
    public static async Task<IReadOnlyList<Guid>> GetTestIdsAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var rows = await executor.ToListAsync(
            context.AsNoTracking(context.SchoolPrograms)
                .Where(sp => sp.SchoolId == schoolId)
                .Join(
                    context.AsNoTracking(context.AssessmentPrograms).Where(p => p.OwnerTestDefinitionId != null),
                    sp => sp.ProgramId,
                    p => p.Id,
                    (sp, p) => new { TestId = p.OwnerTestDefinitionId!.Value, sp.CreatedAt })
                .OrderBy(x => x.CreatedAt),
            cancellationToken).ConfigureAwait(false);

        return rows.Select(r => r.TestId).ToList();
    }

    /// <summary>
    /// Maktabning test biriktirmalarini `testIds` bilan TO'LIQ almashtiradi (saqlamaydi).
    /// Xatolar: test topilmasa `404 NOT_FOUND`; YANGI biriktirilayotgan test arxivlangan bo'lsa
    /// `409 TEST_ARCHIVED` (allaqachon biriktirilgan arxiv test qoldirilishi mumkin — forma
    /// ro'yxatni qaytarib yuborganda maktabni tahrirlash to'xtab qolmasin).
    /// </summary>
    public static async Task<Result> ReplaceAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid schoolId,
        IReadOnlyCollection<Guid> testIds,
        Guid adminUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var targetTestIds = testIds.Distinct().ToList();

        var tests = await executor.ToListAsync(
            context.AsNoTracking(context.TestDefinitions).Where(t => targetTestIds.Contains(t.Id)),
            cancellationToken).ConfigureAwait(false);

        var missing = targetTestIds.Except(tests.Select(t => t.Id)).ToList();
        if (missing.Count > 0)
        {
            return Result.Failure(new Error(
                ProblemCodes.NotFound,
                $"Anketa topilmadi: {string.Join(", ", missing)}.",
                new Dictionary<string, object> { ["testIds"] = missing }));
        }

        // Joriy test-dastur biriktirmalari (tracked — olib tashlash uchun).
        var currentLinks = await executor.ToListAsync(
            context.SchoolPrograms
                .Where(sp => sp.SchoolId == schoolId)
                .Join(
                    context.AssessmentPrograms.Where(p => p.OwnerTestDefinitionId != null),
                    sp => sp.ProgramId,
                    p => p.Id,
                    (sp, p) => new { Link = sp, TestId = p.OwnerTestDefinitionId!.Value }),
            cancellationToken).ConfigureAwait(false);

        var alreadyLinked = currentLinks.Select(l => l.TestId).ToHashSet();

        var archivedNew = tests
            .Where(t => t.Status == TestDefinitionStatus.Archived && !alreadyLinked.Contains(t.Id))
            .Select(t => t.Id)
            .ToList();
        if (archivedNew.Count > 0)
        {
            return Result.Failure(new Error(
                ProblemCodes.TestArchived,
                "Arxivlangan anketani maktabga biriktirib bo'lmaydi.",
                new Dictionary<string, object> { ["testIds"] = archivedNew }));
        }

        var target = targetTestIds.ToHashSet();

        foreach (var row in currentLinks.Where(l => !target.Contains(l.TestId)))
        {
            context.Remove(row.Link);
        }

        foreach (var test in tests.Where(t => !alreadyLinked.Contains(t.Id)).OrderBy(t => targetTestIds.IndexOf(t.Id)))
        {
            var (program, _) = await TestPrograms
                .EnsureAsync(context, executor, test, adminUserId, now, cancellationToken)
                .ConfigureAwait(false);

            context.Add(SchoolProgram.Create(Guid.NewGuid(), schoolId, program.Id, now));
        }

        return Result.Success();
    }
}
