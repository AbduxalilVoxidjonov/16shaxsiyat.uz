namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Kriptografik jihatdan tasodifiy, URL-xavfsiz tokenlar generatsiyasi — maktab havolasi
/// `accessToken`i va o'quvchi `SessionToken`i uchun (`docs/08-auth-va-xavfsizlik.md` 3, 4-bo'lim:
/// "32 bayt `RandomNumberGenerator` → Base64Url"). `Infrastructure` da amalga oshiriladi.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>`byteLength` baytli tasodifiy qiymatni Base64Url (`+`/`/` yo'q, `=` to'ldiruvisiz) shaklda qaytaradi.</summary>
    string GenerateUrlSafeToken(int byteLength);
}
