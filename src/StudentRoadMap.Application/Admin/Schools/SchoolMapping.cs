using StudentRoadMap.Application.Common.Interfaces;
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
        School school, IAppSettings appSettings, IQrCodeGenerator qrCodeGenerator, AdminSchoolStatsDto stats)
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
            stats);
    }

    public static string BuildPublicUrl(School school, IAppSettings appSettings) =>
        $"{appSettings.PublicWebBaseUrl}/t/{school.Slug.Value}?k={school.AccessToken}";

    /// <summary>
    /// `students` jadvalidan snapshot ustunlar orqali (`Assessments`/`TestResults`ga
    /// murojaat qilinmaydi) — `ListSchoolsQueryHandler` izohidagi bilan bir xil sabab.
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

        var lastAssessmentDates = await executor.ToListAsync(
            students.Select(s => s.LastAssessmentAt),
            cancellationToken).ConfigureAwait(false);
        var lastActivityAt = lastAssessmentDates.Count == 0 ? null : lastAssessmentDates.Max();

        return new AdminSchoolStatsDto(studentCount, completedCount, lastActivityAt);
    }
}
