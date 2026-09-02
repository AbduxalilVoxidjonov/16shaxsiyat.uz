using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.CompleteTest;

/// <summary>
/// `POST /api/public/sessions/tests/{testCode}/complete` — `docs/07` 1.7-bo'lim. `AssessmentId`
/// so'rov tanasi/URL'dan EMAS — kontroller `HttpContext.Items["AssessmentId"]`dan oladi
/// (IDOR himoyasi, `CLAUDE.md` 8-qoida), `TestCode` URL segmenti.
/// </summary>
public sealed record CompleteTestCommand(Guid AssessmentId, string TestCode) : IRequest<Result<CompleteTestResult>>;
