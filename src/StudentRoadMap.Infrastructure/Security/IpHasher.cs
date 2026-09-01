using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Security;

/// <summary>
/// IP manzilini `SHA256(ip + Security:IpHashSalt)` bilan bir tomonlama xeshlaydi (`CLAUDE.md`
/// 9-band, `docs/08-auth-va-xavfsizlik.md` 5-bo'lim). Tuz sir sifatida env/user-secrets orqali
/// keladi — kodda yo'q (`CLAUDE.md` 4-qoida).
/// </summary>
internal sealed class IpHasher : IIpHasher
{
    private readonly string _salt;

    public IpHasher(IConfiguration configuration)
    {
        _salt = configuration["Security:IpHashSalt"]
            ?? throw new InvalidOperationException(
                "'Security:IpHashSalt' sozlamasi topilmadi. Env o'zgaruvchi yoki user-secrets orqali bering.");
    }

    public string? Hash(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        var bytes = Encoding.UTF8.GetBytes(ipAddress + _salt);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
