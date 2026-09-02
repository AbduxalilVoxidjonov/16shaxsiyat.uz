using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.RemoveTest;

/// <summary>`DELETE /api/admin/programs/{id}/tests/{testDefinitionId}` — `prompts/34` E15-band.</summary>
public sealed record RemoveProgramTestCommand(
    Guid ProgramId,
    Guid TestDefinitionId,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
