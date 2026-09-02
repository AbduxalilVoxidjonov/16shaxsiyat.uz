using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.RegenerateLink;

/// <summary>
/// `POST /api/admin/schools/{id}/regenerate-link` — `docs/07-api-shartnoma.md` 3.1-bo'lim:
/// yangi `accessToken` → `{ publicUrl, qrCodeBase64 }`. Eski havola DARHOL ishlamay qoladi
/// (`School.RegenerateAccessToken` — bitta ustunni yangilaydi, `docs/04` 2.1 invarianti).
/// </summary>
public sealed record RegenerateSchoolLinkCommand(
    Guid Id,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<RegenerateSchoolLinkResult>>;
