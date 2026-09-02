using StudentRoadMap.Application.Admin.Schools.Update;

namespace StudentRoadMap.Api.Contracts.Admin.Schools;

/// <summary>`PUT /api/admin/schools/{id}` so'rov tanasi — `docs/07` 3.1-bo'lim.</summary>
public sealed record UpdateSchoolRequest(
    string Name,
    string Region,
    string District,
    string? SchoolNumber,
    string? ContactPerson,
    string? ContactPhone,
    string? AccessCode,
    int? DailyRegistrationLimit,
    string? Notes)
{
    public UpdateSchoolCommand ToCommand(Guid id, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(
            id,
            Name,
            Region,
            District,
            SchoolNumber,
            ContactPerson,
            ContactPhone,
            AccessCode,
            DailyRegistrationLimit,
            Notes,
            adminUserId,
            ipAddress,
            userAgent);
}
