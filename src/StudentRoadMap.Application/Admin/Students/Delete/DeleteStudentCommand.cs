using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Students.Delete;

/// <summary>
/// `DELETE /api/admin/students/{id}?hard=true` — `docs/07-api-shartnoma.md` 3.2-bo'lim.
/// `Hard = false` — yumshoq o'chirish (`Student.MarkDeleted`). `Hard = true` — o'quvchi so'rovi
/// bo'yicha TO'LIQ o'chirish: student + sessiyalar + javoblar + AI (`prompts/14` MAXSUS DIQQAT #5).
/// </summary>
public sealed record DeleteStudentCommand(
    Guid Id,
    bool Hard,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result>;
