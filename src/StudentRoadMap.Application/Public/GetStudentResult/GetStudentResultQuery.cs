using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.GetStudentResult;

/// <summary>
/// `GET /api/public/sessions/result` — `docs/07` 1.9-bo'lim. `AssessmentId` `X-Session-Token`
/// orqali autentifikatsiyadan keladi (IDOR himoyasi, `CLAUDE.md` 8-qoida).
/// </summary>
public sealed record GetStudentResultQuery(Guid AssessmentId) : IRequest<Result<GetStudentResultResult?>>;
