using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Delete;

/// <summary>`DELETE /api/admin/catalog/questions/{id}` — `docs/07` §3.4: tizim testida `409 SYSTEM_TEST_LOCKED`.</summary>
public sealed record DeleteTestQuestionCommand(Guid QuestionId, Guid AdminUserId, string? IpAddress = null, string? UserAgent = null) : IRequest<Result>;
