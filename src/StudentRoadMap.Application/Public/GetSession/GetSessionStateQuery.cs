using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.GetSession;

/// <summary>
/// `GET /api/public/sessions/me` — `docs/07-api-shartnoma.md` 1.3-bo'lim. `AssessmentId`
/// so'rov tanasi/URL'dan emas — `SessionTokenAuthenticationHandler` `HttpContext.Items` orqali
/// beradi (IDOR himoyasi, `docs/08` 4-bo'lim: "hech qanday `assessmentId` URL'da qabul qilinmaydi").
/// </summary>
public sealed record GetSessionStateQuery(Guid AssessmentId) : IRequest<Result<GetSessionStateResult>>;
