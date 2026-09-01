namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// IP manzilini xom holda saqlamaslik uchun bir tomonlama xeshlash (`CLAUDE.md` 9-band,
/// `docs/08-auth-va-xavfsizlik.md` 5-bo'lim: `SHA256(ip + Security:IpHashSalt)`). Tuz sir
/// sifatida env orqali keladi — kodda yo'q.
/// </summary>
public interface IIpHasher
{
    /// <summary>`ipAddress` bo'sh/`null` bo'lsa `null` qaytaradi (masalan, test muhitida IP aniqlanmasa).</summary>
    string? Hash(string? ipAddress);
}
