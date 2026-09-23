using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.PublicSpace;

/// <summary>
/// Dastur ↔ makon biriktirmasining (`school_programs`) YAGONA mexanizmi: mavjudlik
/// tekshiruvlari, IDEMPOTENTLIK va audit yozuvi.
///
/// <para>
/// Ikki chaqiruvchi bor va ikkalasi ham AYNAN shu metodlarni ishlatadi:
/// `Admin/Programs` (maktabga biriktirish, `prompts/34` E15 — 2026-09-23 da bo'lim olib tashlandi) va `Admin/PublicSpace`
/// (ommaviy makonga biriktirish, 2026-09-06). Ular faqat JAVOB shaklida farq qiladi
/// (`AdminProgramDetailDto` / `AdminPublicSpaceDto`).
/// </para>
/// <para>
/// **Nima uchun handler ichidan `ISender.Send(...)` bilan qayta ishlatilmadi:**
/// `TransactionBehavior` nomi `Command` bilan tugaydigan HAR SO'ROVNI o'z tranzaksiyasiga
/// o'raydi — command ichidan command yuborilsa, allaqachon ochiq tranzaksiya ustiga
/// ikkinchisi ochilib, EF Core `InvalidOperationException` otadi (500). Shu sabab qayta
/// ishlatish MediatR darajasida emas, mana shu sof Application servisi darajasida.
/// </para>
/// </summary>
internal static class ProgramSchoolAssignment
{
    /// <summary>
    /// Biriktiradi. Idempotent — allaqachon biriktirilgan bo'lsa hech narsa qilinmaydi
    /// (`409` EMAS) va audit yozuvi ham takrorlanmaydi.
    /// Muvaffaqiyatda topilgan dastur qaytariladi (chaqiruvchi javob DTO'sini shundan quradi).
    /// </summary>
    public static async Task<Result<AssessmentProgram>> AssignAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IIpHasher ipHasher,
        Guid programId,
        Guid schoolId,
        Guid adminUserId,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var program = await FindProgramAsync(context, executor, programId, cancellationToken).ConfigureAwait(false);

        if (program is null)
        {
            return Result.Failure<AssessmentProgram>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        // Ommaviy makon ham `schools` jadvalidagi qator — bu yerda `AdminSchoolScope` ATAYLAB
        // QO'LLANMAYDI: bu mexanizm ikkala makon turiga ham xizmat qiladi (ko'lam ajratish
        // faqat admin "Maktablar" BO'LIMIning ishi).
        var schoolExists = await executor.AnyAsync(
            context.Schools.Where(s => s.Id == schoolId),
            cancellationToken).ConfigureAwait(false);

        if (!schoolExists)
        {
            return Result.Failure<AssessmentProgram>(new Error(ProblemCodes.NotFound, "Maktab topilmadi."));
        }

        var alreadyAssigned = await executor.AnyAsync(
            context.SchoolPrograms.Where(sp => sp.ProgramId == programId && sp.SchoolId == schoolId),
            cancellationToken).ConfigureAwait(false);

        if (!alreadyAssigned)
        {
            context.Add(SchoolProgram.Create(Guid.NewGuid(), schoolId, programId, now));

            context.Add(AuditLog.Create(
                AuditActions.ProgramSchoolAssigned,
                now,
                adminUserId,
                entityType: "AssessmentProgram",
                entityId: program.Id,
                afterJson: AuditSnapshot.Serialize(new { program.Id, SchoolId = schoolId }),
                ipHash: ipHasher.Hash(ipAddress),
                userAgent: userAgent));

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(program);
    }

    /// <summary>Biriktirmani olib tashlaydi. Idempotent — biriktirilmagan bo'lsa `409` emas.</summary>
    public static async Task<Result<AssessmentProgram>> UnassignAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IIpHasher ipHasher,
        Guid programId,
        Guid schoolId,
        Guid adminUserId,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var program = await FindProgramAsync(context, executor, programId, cancellationToken).ConfigureAwait(false);

        if (program is null)
        {
            return Result.Failure<AssessmentProgram>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        var link = await executor.FirstOrDefaultAsync(
            context.SchoolPrograms.Where(sp => sp.ProgramId == programId && sp.SchoolId == schoolId),
            cancellationToken).ConfigureAwait(false);

        if (link is not null)
        {
            context.Remove(link);

            context.Add(AuditLog.Create(
                AuditActions.ProgramSchoolUnassigned,
                now,
                adminUserId,
                entityType: "AssessmentProgram",
                entityId: program.Id,
                afterJson: AuditSnapshot.Serialize(new { program.Id, SchoolId = schoolId }),
                ipHash: ipHasher.Hash(ipAddress),
                userAgent: userAgent));

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(program);
    }

    private static async Task<AssessmentProgram?> FindProgramAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid programId,
        CancellationToken cancellationToken) =>
        await executor.FirstOrDefaultAsync(
            context.AsNoTracking(context.AssessmentPrograms).Where(p => p.Id == programId),
            cancellationToken).ConfigureAwait(false);
}
