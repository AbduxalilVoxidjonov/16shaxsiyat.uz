using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.Publish;

/// <summary>`POST /api/admin/programs/{id}/publish` — `prompts/34` E15-band. `Draft ──▶ Published`.</summary>
public sealed record PublishProgramCommand(
    Guid Id,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
