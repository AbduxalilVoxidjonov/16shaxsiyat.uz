using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.StartTest;

/// <summary>
/// `POST /api/public/sessions/tests/{testCode}/start` — `docs/07` 1.4-bo'lim. `AssessmentId`
/// so'rov tanasi/URL'dan EMAS — kontroller uni `SessionTokenAuthenticationHandler` autentifikatsiya
/// paytida `HttpContext.Items["AssessmentId"]`ga qo'ygan qiymatdan oladi (IDOR himoyasi,
/// `CLAUDE.md` 8-qoida). `TestCode` esa URL segmenti.
/// </summary>
public sealed record StartTestCommand(Guid AssessmentId, string TestCode) : IRequest<Result<StartTestResult>>;
