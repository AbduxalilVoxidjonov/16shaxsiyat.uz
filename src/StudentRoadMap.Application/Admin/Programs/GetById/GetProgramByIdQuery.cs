using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.GetById;

/// <summary>`GET /api/admin/programs/{id}` — `prompts/34` E15-band.</summary>
public sealed record GetProgramByIdQuery(Guid Id) : IRequest<Result<AdminProgramDetailDto>>;
