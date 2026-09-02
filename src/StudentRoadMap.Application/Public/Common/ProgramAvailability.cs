using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// Maktab uchun mavjud dasturlar ro'yxati (`docs/06` 8-bo'lim, 2026-09-02 qaror, `prompts/34`
/// C8/C9-band): `Visibility = Public` **yoki** `school_programs` orqali biriktirilgan, faqat
/// `Status = Published &amp;&amp; IsActive`. `GetSchoolInfoQueryHandler` (boshlanish ekrani) va
/// `StartSessionCommandHandler` (dastur tanlash mantiqi) BIR XIL ushbu so'rovdan foydalanadi —
/// ikkalasi turli joyda mos kelmaydigan ro'yxat qaytarishi mumkin emas.
/// </summary>
internal static class ProgramAvailability
{
    public static async Task<IReadOnlyList<AssessmentProgram>> GetAvailableProgramsAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var assignedProgramIds = await executor.ToListAsync(
            context.AsNoTracking(context.SchoolPrograms).Where(sp => sp.SchoolId == schoolId).Select(sp => sp.ProgramId),
            cancellationToken).ConfigureAwait(false);

        return await executor.ToListAsync(
            context.AsNoTracking(context.AssessmentPrograms)
                .Where(p => p.Status == ProgramStatus.Published
                    && p.IsActive
                    && (p.Visibility == ProgramVisibility.Public || assignedProgramIds.Contains(p.Id)))
                .OrderBy(p => p.DisplayOrder),
            cancellationToken).ConfigureAwait(false);
    }
}
