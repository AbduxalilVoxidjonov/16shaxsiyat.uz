using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.Archive;

/// <summary>`POST /api/admin/programs/{id}/archive` — `prompts/34` E15-band. `Draft`/`Published` ──▶ `Archived`.</summary>
public sealed record ArchiveProgramCommand(
    Guid Id,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
