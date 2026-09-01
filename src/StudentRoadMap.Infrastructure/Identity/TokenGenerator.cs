using System.Security.Cryptography;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Identity;

/// <summary>
/// `RandomNumberGenerator` asosida Base64Url token generatsiyasi — maktab `accessToken`i va
/// o'quvchi `SessionToken`i uchun (`docs/08-auth-va-xavfsizlik.md` 3, 4-bo'lim).
/// </summary>
internal sealed class TokenGenerator : ITokenGenerator
{
    public string GenerateUrlSafeToken(int byteLength)
    {
        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Bayt uzunligi musbat bo'lishi kerak.");
        }

        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
