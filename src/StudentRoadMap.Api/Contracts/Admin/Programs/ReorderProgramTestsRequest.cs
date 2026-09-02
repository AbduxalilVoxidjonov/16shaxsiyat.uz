using StudentRoadMap.Application.Admin.Programs.ReorderTests;

namespace StudentRoadMap.Api.Contracts.Admin.Programs;

/// <summary>`POST /api/admin/programs/{id}/tests/reorder` so'rov tanasi — `prompts/34` E15-band.</summary>
public sealed record ReorderProgramTestsRequest(IReadOnlyList<Guid> TestDefinitionIds)
{
    public ReorderProgramTestsCommand ToCommand(Guid programId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(programId, TestDefinitionIds, adminUserId, ipAddress, userAgent);
}
