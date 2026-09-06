using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.Restore;

/// <summary>
/// `POST /api/admin/programs/{id}/restore` — `Archived` ──▶ `Paused` (2026-09-06, egasining
/// so'rovi: arxivlangan dasturni nusxa olmasdan qaytarish). Nega `Active` emas —
/// `AssessmentProgram.Restore` izohi.
/// </summary>
public sealed record RestoreProgramCommand(
    Guid Id,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
