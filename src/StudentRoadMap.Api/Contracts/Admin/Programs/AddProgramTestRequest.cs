using StudentRoadMap.Application.Admin.Programs.AddTest;

namespace StudentRoadMap.Api.Contracts.Admin.Programs;

/// <summary>`POST /api/admin/programs/{id}/tests` so'rov tanasi — `prompts/34` E15-band.</summary>
public sealed record AddProgramTestRequest(Guid TestDefinitionId, int DisplayOrder)
{
    public AddProgramTestCommand ToCommand(Guid programId, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(programId, TestDefinitionId, DisplayOrder, adminUserId, ipAddress, userAgent);
}
