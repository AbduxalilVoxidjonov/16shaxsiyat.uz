using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.RegenerateEntryCode;

/// <summary>
/// `POST /api/admin/schools/{id}/regenerate-entry-code` — `docs/07` 3.1-bo'lim: yangi maktab
/// kodi → `{ entryCode }` (formatlangan, `XXXX-XXXX`). Eski kod DARHOL ishlamay qoladi
/// (`School.RegenerateEntryCode`; `RegenerateSchoolLinkCommand` bilan bir xil naqsh).
/// </summary>
public sealed record RegenerateSchoolEntryCodeCommand(
    Guid Id,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<RegenerateSchoolEntryCodeResult>>;
