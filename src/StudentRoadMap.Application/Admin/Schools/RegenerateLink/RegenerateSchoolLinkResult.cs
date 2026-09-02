namespace StudentRoadMap.Application.Admin.Schools.RegenerateLink;

/// <summary>`docs/07` 3.1-bo'lim: `{ publicUrl, qrCodeBase64 }`. `qrCodeBase64` — xom PNG Base64 (data URI'siz).</summary>
public sealed record RegenerateSchoolLinkResult(string PublicUrl, string QrCodeBase64);
