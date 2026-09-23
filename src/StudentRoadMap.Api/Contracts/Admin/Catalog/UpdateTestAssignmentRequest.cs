using StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;

namespace StudentRoadMap.Api.Contracts.Admin.Catalog;

/// <summary>
/// `PUT /api/admin/catalog/tests/{id}/assignment` so'rov tanasi — `docs/07` §3.4.1 (2026-09-23).
/// `SchoolIds` `null` kelsa bo'sh ro'yxat deb olinadi (hamma maktabdan olib tashlash).
/// `RegistrationMode` berilmasa `Full`. `IsInPublicSpace` berilmasa — o'zgarmaydi.
/// </summary>
public sealed record UpdateTestAssignmentRequest(
    bool IsPublic,
    IReadOnlyList<Guid>? SchoolIds,
    string? RegistrationMode = null,
    bool? IsInPublicSpace = null)
{
    public UpdateTestAssignmentCommand ToCommand(Guid testDefinitionId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(
            testDefinitionId,
            IsPublic,
            SchoolIds ?? [],
            string.IsNullOrWhiteSpace(RegistrationMode) ? "Full" : RegistrationMode,
            IsInPublicSpace,
            adminUserId,
            ipAddress,
            userAgent);
}
