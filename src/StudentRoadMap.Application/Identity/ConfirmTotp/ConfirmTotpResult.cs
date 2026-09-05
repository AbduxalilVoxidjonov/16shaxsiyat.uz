namespace StudentRoadMap.Application.Identity.ConfirmTotp;

/// <summary>
/// `POST /api/auth/totp/confirm` javobi. `BackupCodes` — 8 ta bir martalik kod, **faqat shu
/// javobda bir marta** ochiq matnda ko'rsatiladi (DB'da faqat xeshi saqlanadi, `docs/08`
/// 2-bo'lim). Ular ATAYIN `enable` emas, aynan shu yerda beriladi — sabab
/// <see cref="EnableTotp.EnableTotpResult"/> izohida.
/// </summary>
public sealed record ConfirmTotpResult(IReadOnlyList<string> BackupCodes);
