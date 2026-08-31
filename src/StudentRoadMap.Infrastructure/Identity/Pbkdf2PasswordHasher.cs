using System.Globalization;
using System.Security.Cryptography;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Identity;

/// <summary>
/// PBKDF2-HMACSHA256 asosidagi parol xeshlash (`docs/08-auth-va-xavfsizlik.md`). Har bir parol
/// uchun tasodifiy tuz generatsiya qilinadi; natija `{iterations}.{saltBase64}.{hashBase64}`
/// formatida saqlanadi — versiyalash uchun iteratsiyalar soni ham xeshda qoladi.
/// </summary>
internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 210_000;

    /// <summary>
    /// `Verify`da xeshdan o'qilgan iteratsiyalar soni shu oraliqda bo'lishi shart — aks holda
    /// xesh yaroqsiz deb hisoblanadi. Pastki chegara downgrade hujumidan (zaif xesh bilan
    /// almashtirib qo'yish), yuqori chegara esa DoS'dan (sun'iy katta qiymat bilan CPU'ni band
    /// qilish) himoya qiladi.
    /// </summary>
    private const int MinIterations = 100_000;
    private const int MaxIterations = 1_000_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSizeBytes);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        var parts = hash.Split('.', 3);
        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations))
        {
            return false;
        }

        if (iterations < MinIterations || iterations > MaxIterations)
        {
            // Downgrade (juda kam iteratsiya) yoki DoS (sun'iy katta iteratsiya) urinishi —
            // xesh yaroqsiz deb hisoblanadi, `Rfc2898DeriveBytes.Pbkdf2` chaqirilmaydi.
            return false;
        }

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (expectedHash.Length == 0)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
