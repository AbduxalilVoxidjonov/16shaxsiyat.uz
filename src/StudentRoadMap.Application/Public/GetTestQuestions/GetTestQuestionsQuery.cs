using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.GetTestQuestions;

/// <summary>
/// `GET /api/public/sessions/tests/{testCode}/questions?page=N` — `docs/07` 1.5-bo'lim.
/// `AssessmentId` tokendan (IDOR himoyasi), `TestCode` URL segmentidan, `Page` query'dan.
/// </summary>
public sealed record GetTestQuestionsQuery(Guid AssessmentId, string TestCode, int Page) : IRequest<Result<GetTestQuestionsResult>>;
