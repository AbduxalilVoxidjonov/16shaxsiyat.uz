using MediatR;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Students.GetById;

/// <summary>`GET /api/admin/students/{id}` — `docs/07-api-shartnoma.md` 3.2-bo'lim (individual profil).</summary>
public sealed record GetStudentByIdQuery(Guid Id) : IRequest<Result<AdminStudentProfileDto>>;
