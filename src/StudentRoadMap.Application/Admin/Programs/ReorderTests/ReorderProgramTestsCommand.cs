using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.ReorderTests;

/// <summary>
/// `POST /api/admin/programs/{id}/tests/reorder` — `prompts/34` E15-band. `TestDefinitionIds` —
/// dastur tarkibi bilan AYNAN bir xil to'plam, yangi tartibda (`AssessmentProgram.ReorderTests`).
/// </summary>
public sealed record ReorderProgramTestsCommand(
    Guid ProgramId,
    IReadOnlyList<Guid> TestDefinitionIds,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
