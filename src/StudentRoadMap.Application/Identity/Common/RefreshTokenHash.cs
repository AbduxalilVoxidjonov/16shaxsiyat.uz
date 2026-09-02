using System.Security.Cryptography;
using System.Text;

namespace StudentRoadMap.Application.Identity.Common;

/// <summary>
/// Refresh tokenni DB'da saqlash uchun bir tomonlama xeshlaydi (`docs/08-auth-va-xavfsizlik.md`
/// 2-bo'lim: "DB'da faqat SHA-256 xeshi saqlanadi"). Solishtirish oddiy tenglik orqali (indeks
/// bo'yicha qidiruv) — bu SHA-256 preimage qarshiligiga tayanadi, parol kabi tuzli/qimmat
/// xeshlash shart emas (token o'zi 64 baytli yuqori entropiyali tasodifiy qiymat).
/// </summary>
internal static class RefreshTokenHash
{
    public static string Compute(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}
