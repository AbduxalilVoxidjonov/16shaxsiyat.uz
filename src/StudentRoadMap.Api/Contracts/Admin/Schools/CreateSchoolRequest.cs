using StudentRoadMap.Application.Admin.Schools.Create;

namespace StudentRoadMap.Api.Contracts.Admin.Schools;

/// <summary>
/// `POST /api/admin/schools` so'rov tanasi — `docs/07` 3.1-bo'lim. `Slug` bu yerda YO'Q
/// (avtomatik generatsiya qilinadi, `StartSessionRequest` bilan bir xil "wire shape" naqshi).
/// </summary>
public sealed record CreateSchoolRequest(
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
    public CreateSchoolCommand ToCommand(Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(
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
