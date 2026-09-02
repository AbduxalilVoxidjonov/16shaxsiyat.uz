namespace StudentRoadMap.Application.Identity.EnableTotp;

public sealed record EnableTotpResult(string Secret, string OtpauthUri, IReadOnlyList<string> BackupCodes);
