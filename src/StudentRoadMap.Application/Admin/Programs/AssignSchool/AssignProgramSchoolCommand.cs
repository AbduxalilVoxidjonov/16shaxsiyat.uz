using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.AssignSchool;

/// <summary>
/// `POST /api/admin/programs/{id}/schools/{schoolId}` — `prompts/34` E15-band. Faqat
/// `Visibility = Assigned` dasturlar uchun ma'noga ega (`Public` dasturlar bu jadvalsiz ham
/// barcha maktabda ko'rinadi) — lekin taqiqlanmaydi (admin `Visibility`ni keyinroq o'zgartirishi
/// mumkin, biriktiruv oldindan tayyor turishi zararsiz).
/// </summary>
public sealed record AssignProgramSchoolCommand(
    Guid ProgramId,
    Guid SchoolId,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
