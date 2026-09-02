using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.CompleteSession;

/// <summary>
/// `POST /api/public/sessions/complete` — `docs/07` 1.8-bo'lim. `AssessmentId` so'rov
/// tanasidan EMAS — kontroller `HttpContext.Items["AssessmentId"]`dan oladi (IDOR himoyasi,
/// `CLAUDE.md` 8-qoida).
/// </summary>
public sealed record CompleteSessionCommand(Guid AssessmentId) : IRequest<Result<CompleteSessionResult>>;
