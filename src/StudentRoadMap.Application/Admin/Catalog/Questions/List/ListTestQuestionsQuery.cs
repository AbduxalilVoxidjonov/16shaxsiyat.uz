using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.List;

/// <summary>`GET /api/admin/catalog/tests/{id}/questions` — `docs/07` §3.4 (tizim testida ham ✅).</summary>
public sealed record ListTestQuestionsQuery(Guid TestDefinitionId) : IRequest<Result<IReadOnlyList<CatalogQuestionItemDto>>>;
