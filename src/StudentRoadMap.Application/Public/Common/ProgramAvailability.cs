using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// Maktab uchun mavjud dasturlar ro'yxati (`docs/06` 8-bo'lim, 2026-09-02 qaror, `prompts/34`
/// C8/C9-band): `Visibility = Public` **yoki** `school_programs` orqali biriktirilgan, faqat
/// `Status = Published &amp;&amp; IsActive`. `GetSchoolInfoQueryHandler` (boshlanish ekrani) va
/// `StartSessionCommandHandler` (dastur tanlash mantiqi) BIR XIL ushbu so'rovdan foydalanadi —
/// ikkalasi turli joyda mos kelmaydigan ro'yxat qaytarishi mumkin emas.
///
/// **2026-09-03 (jonli hodisa):** yagona dastur `is_active = false` + `Visibility = Assigned`
/// (0 ta maktab) qilib qo'yilgani uchun BARCHA maktab havolasi jimgina o'lik bo'lib qoldi,
/// admin panelida esa hech qanday belgi yo'q edi. Shundan keyin ayni shu mezon admin tomonida
/// ham ishlatiladi (`Admin/Schools/LinkHealth/SchoolLinkHealthEvaluator`) — panel "hammasi
/// joyida" deb yolg'on aytmasligi uchun. Mezon IKKI SHAKLDA yozilgan:
///
/// 1. <see cref="Filter"/> — EF Core `Where` uchun ifoda daraxti (DB darajasida);
/// 2. <see cref="Matches"/> — xotirada, allaqachon o'qilgan katalog ustidan (admin batch
///    hisobi N+1 qilmasligi uchun).
///
/// Ikkalasi bir xil ekanligi `ProgramAvailabilityCriterionTests` da (Status × IsActive ×
/// Visibility × assigned to'liq dekart ko'paytmasi) qulflangan — biri o'zgarsa test yiqiladi.
/// </summary>
internal static class ProgramAvailability
{
    /// <summary>
    /// EF Core uchun mezon (DB darajasida). `assignedProgramIds` — shu maktabga
    /// `school_programs` orqali biriktirilgan dastur ID'lari.
    /// </summary>
    public static System.Linq.Expressions.Expression<Func<AssessmentProgram, bool>> Filter(
        IReadOnlyCollection<Guid> assignedProgramIds) =>
        p => p.Status == ProgramStatus.Published
            && p.IsActive
            && (p.Visibility == ProgramVisibility.Public || assignedProgramIds.Contains(p.Id));

    /// <summary>
    /// <see cref="Filter"/> ning xotiradagi ayni ekvivalenti — bitta dastur shu maktab uchun
    /// mavjudmi. `assignedToSchool` — dastur shu maktabga biriktirilganmi.
    /// </summary>
    public static bool Matches(AssessmentProgram program, bool assignedToSchool) =>
        program.Status == ProgramStatus.Published
        && program.IsActive
        && (program.Visibility == ProgramVisibility.Public || assignedToSchool);

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
                .Where(Filter(assignedProgramIds))
                .OrderBy(p => p.DisplayOrder),
            cancellationToken).ConfigureAwait(false);
    }
}
