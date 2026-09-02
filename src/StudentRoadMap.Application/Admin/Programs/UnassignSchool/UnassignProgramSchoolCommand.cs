using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.UnassignSchool;

/// <summary>`DELETE /api/admin/programs/{id}/schools/{schoolId}` — `prompts/34` E15-band.</summary>
public sealed record UnassignProgramSchoolCommand(
    Guid ProgramId,
    Guid SchoolId,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
