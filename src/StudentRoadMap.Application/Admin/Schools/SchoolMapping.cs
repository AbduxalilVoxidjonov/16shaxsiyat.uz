using StudentRoadMap.Application.Admin.Dashboard;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.Schools;

/// <summary>
/// `AdminSchoolDetailDto` qurish uchun umumiy mantiq — `GetSchoolByIdQueryHandler`,
/// `CreateSchoolCommandHandler`, `UpdateSchoolCommandHandler`, `ToggleSchoolActiveCommandHandler`
/// bir xil statistika/`publicUrl` hisoblashini takrorlamasligi uchun.
/// </summary>
internal static class SchoolMapping
{
    public static AdminSchoolDetailDto ToDetailDto(
        School school,
        IAppSettings appSettings,
        IQrCodeGenerator qrCodeGenerator,
        AdminSchoolStatsDto stats,
        AdminSchoolLinkHealthDto linkHealth)
    {
        var publicUrl = BuildPublicUrl(school, appSettings);

        return new AdminSchoolDetailDto(
            school.Id,
            school.Name,
            school.Region,
            school.District,
            school.SchoolNumber,
            school.ContactPerson,
            school.ContactPhone,
            school.Slug.Value,
            publicUrl,
            qrCodeGenerator.GeneratePngBase64(publicUrl),
            school.AccessCode,
            school.DailyRegistrationLimit,
            school.IsActive,
            school.Notes,
            school.CreatedAt,
            school.UpdatedAt,
            stats,
            linkHealth);
    }

    public static string BuildPublicUrl(School school, IAppSettings appSettings) =>
        $"{appSettings.PublicWebBaseUrl}/t/{school.Slug.Value}?k={school.AccessToken}";

    /// <summary>
    /// Ro'yxatdan o'tgan/yakunlagan/oxirgi faollik — `students` jadvalidan snapshot ustunlar
    /// orqali (`TestResults`ga murojaat qilinmaydi) — `ListSchoolsQueryHandler` izohidagi bilan
    /// bir xil sabab.
    ///
    /// **`InProgressCount` uchun BITTA qo'shimcha agregat so'rov** (`COUNT(*)` — qatorlar
    /// o'qilmaydi, N+1 YO'Q: bitta maktab uchun jami 4 ta agregat/proyeksiya so'rovi, o'quvchi
    /// yoki sessiya bo'yicha SIKL yo'q). "Jarayonda" ni `students` snapshotidan hisoblab
    /// bo'lmaydi — snapshot faqat YAKUNLANGAN sessiyada yangilanadi (`Student.UpdateSnapshot`),
    /// shu sabab yagona manba `assessments.status`.
    ///
    /// `IsDeleted` yozuvlar (o'quvchi ham, sessiya ham) EF Core global query filtri bilan
    /// avtomatik chiqarib tashlanadi (`AppDbContext.OnModelCreating`) — bu yerda qo'lda shart
    /// qo'shilmaydi (`IgnoreQueryFilters` ham chaqirilmaydi).
    /// </summary>
    public static async Task<AdminSchoolStatsDto> ComputeStatsAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        var students = context.AsNoTracking(context.Students).Where(s => s.SchoolId == schoolId);

        var studentCount = await executor.CountAsync(students, cancellationToken).ConfigureAwait(false);
        var completedCount = await executor.CountAsync(
            students.Where(s => s.CompletedAssessmentCount > 0),
            cancellationToken).ConfigureAwait(false);

        // `GROUP BY ... MAX(last_assessment_at)` SQLite'da (sinov muhiti) `DateTimeOffset` uchun
        // tarjima qilinmaydi — `ListSchoolsQueryHandler`dagi bilan BIR XIL ildiz sabab, shu sabab
        // faqat SHU maktabning sanalari o'qilib, maksimum xotirada topiladi.
        var lastAssessmentDates = await executor.ToListAsync(
            students.Select(s => s.LastAssessmentAt),
            cancellationToken).ConfigureAwait(false);
        var lastActivityAt = lastAssessmentDates.Count == 0 ? null : lastAssessmentDates.Max();

        var inProgressCount = await executor.CountAsync(
            context.AsNoTracking(context.Assessments)
                .Where(a => a.SchoolId == schoolId && a.Status == AssessmentStatus.InProgress),
            cancellationToken).ConfigureAwait(false);

        return new AdminSchoolStatsDto(
            studentCount,
            completedCount,
            inProgressCount,
            AdminDashboardMath.CompletionRate(completedCount, studentCount),
            lastActivityAt);
    }
}
