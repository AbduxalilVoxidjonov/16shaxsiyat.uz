using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.ToggleActive;

/// <summary>`POST /api/admin/programs/{id}/toggle-active` — `prompts/34` E15-band. Joriy holatning teskarisiga o'tkazadi.</summary>
public sealed record ToggleProgramActiveCommand(
    Guid Id,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
