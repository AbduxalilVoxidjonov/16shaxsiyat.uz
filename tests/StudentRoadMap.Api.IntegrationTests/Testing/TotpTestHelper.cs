using System.Globalization;
using System.Security.Cryptography;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>
/// RFC 6238/4226 TOTP kodini MUSTAQIL (`TotpService` amalga oshirilishiga bog'liq bo'lmagan)
/// hisoblaydi — integratsiya testlarida `EnableTotp` javobidagi Base32 sekretdan haqiqiy 6
/// xonali kod olish uchun.
/// </summary>
internal static class TotpTestHelper
{
    public static string ComputeCode(string base32Secret, DateTimeOffset now, int stepOffset = 0)
    {
        var step = (now.ToUnixTimeSeconds() / 30) + stepOffset;
        var secretBytes = Base32Decode(base32Secret);

        var counter = new byte[8];
        var value = step;
        for (var i = counter.Length - 1; i >= 0; i--)
        {
            counter[i] = (byte)(value & 0xFF);
            value >>= 8;
        }

        var hash = HMACSHA1.HashData(secretBytes, counter);
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);
        var otp = binary % 1_000_000;

        return otp.ToString("D6", CultureInfo.InvariantCulture);
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        var bitBuffer = 0;
        var bitCount = 0;

        foreach (var c in base32.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var index = alphabet.IndexOf(c);
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
