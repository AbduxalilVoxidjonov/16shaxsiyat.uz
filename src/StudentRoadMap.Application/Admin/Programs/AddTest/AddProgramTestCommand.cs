using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.AddTest;

/// <summary>
/// `POST /api/admin/programs/{id}/tests` — `prompts/34` E15-band. Tizim dasturida
/// `409 SYSTEM_PROGRAM_LOCKED` (`AssessmentProgram.AddTest` domen qo'riqchisi).
/// </summary>
public sealed record AddProgramTestCommand(
    Guid ProgramId,
    Guid TestDefinitionId,
    int DisplayOrder,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
