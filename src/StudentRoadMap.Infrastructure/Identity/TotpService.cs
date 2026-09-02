using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Identity;

/// <summary>
/// RFC 6238 (TOTP, HMAC-SHA1 asosida) — `docs/08-auth-va-xavfsizlik.md` 2-bo'lim: "30 s oyna
/// ±1". 6 xonali kod (sanoat standarti — Google Authenticator va h.k.). Uchinchi tomon paketi
/// ishlatilmaydi — algoritm oddiy va tekshiruv kutubxonasiz to'liq test qilinadi.
/// </summary>
internal sealed class TotpService : ITotpService
{
    private const int SecretByteLength = 20;
    private const int StepSeconds = 30;
    private const int Digits = 6;
    private const int WindowSteps = 1;

    public string GenerateSecret() => Base32.Encode(RandomNumberGenerator.GetBytes(SecretByteLength));

    public string BuildOtpauthUri(string secretBase32, string accountName, string issuer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretBase32);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);

        var label = Uri.EscapeDataString($"{issuer}:{accountName}");
        var encodedIssuer = Uri.EscapeDataString(issuer);

        return $"otpauth://totp/{label}?secret={secretBase32}&issuer={encodedIssuer}&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
    }

    public bool TryValidate(string secretBase32, string code, DateTimeOffset now, long? lastUsedStep, out long matchedStep)
    {
        matchedStep = 0;

        if (string.IsNullOrWhiteSpace(secretBase32) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = code.Trim();
        if (normalizedCode.Length != Digits || !normalizedCode.All(char.IsDigit))
        {
            return false;
        }

        byte[] secretBytes;
        try
        {
            secretBytes = Base32.Decode(secretBase32);
        }
        catch (FormatException)
        {
            return false;
        }

        var currentStep = now.ToUnixTimeSeconds() / StepSeconds;
        var minAcceptableStep = lastUsedStep ?? long.MinValue;

        for (var delta = -WindowSteps; delta <= WindowSteps; delta++)
        {
            var step = currentStep + delta;

            // Qayta ishlatishga qarshi himoya: oldin qabul qilingan (yoki undan eskiroq) qadam
            // qayta qabul qilinmaydi (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 5-band).
            if (step <= minAcceptableStep)
            {
                continue;
            }

            var candidate = ComputeCode(secretBytes, step);
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(candidate), Encoding.ASCII.GetBytes(normalizedCode)))
            {
                matchedStep = step;
                return true;
            }
        }

        return false;
    }

    /// <summary>RFC 4226 HOTP — `step` hisoblagich sifatida (big-endian, 8 bayt).</summary>
    private static string ComputeCode(byte[] secretBytes, long step)
    {
        var counter = new byte[8];
        for (var i = counter.Length - 1; i >= 0; i--)
        {
            counter[i] = (byte)(step & 0xFF);
            step >>= 8;
        }

        var hash = HMACSHA1.HashData(secretBytes, counter);

        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var otp = binary % (int)Math.Pow(10, Digits);
        return otp.ToString(new string('0', Digits), CultureInfo.InvariantCulture);
    }
}

/// <summary>RFC 4648 Base32 (to'ldiruvchisiz, katta harf) — TOTP sekretlarini kodlash/dekodlash uchun.</summary>
internal static class Base32
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder((data.Length * 8 + 4) / 5);
        var bitBuffer = 0;
        var bitCount = 0;

        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;

            while (bitCount >= 5)
            {
                bitCount -= 5;
                builder.Append(Alphabet[(bitBuffer >> bitCount) & 0x1F]);
            }
        }

        if (bitCount > 0)
        {
            builder.Append(Alphabet[(bitBuffer << (5 - bitCount)) & 0x1F]);
        }

        return builder.ToString();
    }

    public static byte[] Decode(string base32)
    {
        var normalized = base32.Trim().TrimEnd('=').ToUpperInvariant();
        if (normalized.Length == 0)
        {
            return [];
        }

        var bytes = new List<byte>((normalized.Length * 5) / 8);
        var bitBuffer = 0;
        var bitCount = 0;

        foreach (var c in normalized)
        {
            var index = Alphabet.IndexOf(c);
            if (index < 0)
            {
                throw new FormatException($"Base32 formatida yaroqsiz belgi: '{c}'.");
            }

            bitBuffer = (bitBuffer << 5) | index;
            bitCount += 5;

            if (bitCount >= 8)
            {
                bitCount -= 8;
                bytes.Add((byte)((bitBuffer >> bitCount) & 0xFF));
            }
        }

        return [.. bytes];
    }
}
