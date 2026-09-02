using MediatR;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.GetById;

/// <summary>`GET /api/admin/schools/{id}` — `docs/07-api-shartnoma.md` 3.1-bo'lim.</summary>
public sealed record GetSchoolByIdQuery(Guid Id) : IRequest<Result<AdminSchoolDetailDto>>;
