using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.ListAssessments;

/// <summary>`GET /api/me/assessments` — foydalanuvchining test sessiyalari tarixi.</summary>
public sealed record ListMyAssessmentsQuery(Guid PublicUserId) : IRequest<Result<ListMyAssessmentsResult>>;
