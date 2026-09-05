using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Identity;

/// <summary>
/// Telegram Login Widget imzosini tekshiradi (Telegram rasmiy algoritmi,
/// `docs/08-auth-va-xavfsizlik.md` 2a-bo'lim).
///
/// Bot tokeni SIR — `Telegram:BotToken` (env `Telegram__BotToken`), kodda/`appsettings.json`da
/// YO'Q (`CLAUDE.md` 4-qoida). Token berilmasa istisno OTILMAYDI (`JwtTokenService` dagi
/// fail-fast naqshi BU YERGA qo'llanmaydi): Telegram kirishi ixtiyoriy imkoniyat, u
/// sozlanmagani uchun butun ilova ishga tushmay qolishi noto'g'ri bo'lardi — o'rniga
/// <see cref="IsConfigured"/> `false` bo'ladi va endpoint `503 TELEGRAM_AUTH_NOT_CONFIGURED`
/// qaytaradi.
///
/// **Sinovlar uchun eslatma:** `Verify` mutlaqo determinstik va tashqi bog'liqliksiz —
/// tokendan `secret_key` hisoblab, HMAC ni doimiy vaqtda solishtiradi.
/// </summary>
internal sealed class TelegramLoginVerifier : ITelegramLoginVerifier
{
    /// <summary>`hash` maydoni `data_check_string` ga KIRMAYDI (Telegram algoritmi).</summary>
    private const string HashFieldName = "hash";

    /// <summary>`secret_key = SHA256(bot_token)` — bir marta hisoblanadi (servis `Singleton`).</summary>
    private readonly byte[]? _secretKey;

    public TelegramLoginVerifier(IConfiguration configuration)
    {
        var botToken = configuration["Telegram:BotToken"];

        _secretKey = string.IsNullOrWhiteSpace(botToken)
            ? null
            : SHA256.HashData(Encoding.UTF8.GetBytes(botToken));
    }

    public bool IsConfigured => _secretKey is not null;

    public bool Verify(IReadOnlyDictionary<string, string> fields, string hash)
    {
        ArgumentNullException.ThrowIfNull(fields);

        if (_secretKey is null || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        // `data_check_string` — kalit bo'yicha ALIFBO (ordinal) tartibida saralangan
        // "key=value" qatorlari, "\n" bilan birlashtirilgan. `hash` chiqarib tashlanadi.
        var dataCheckString = string.Join(
            '\n',
            fields
                .Where(pair => !string.Equals(pair.Key, HashFieldName, StringComparison.Ordinal))
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"));

        var computed = HMACSHA256.HashData(_secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        // Kutilgan `hash` — hex satr. `Convert.FromHexString` noto'g'ri formatda istisno
        // otadi — mijoz yuborgan qiymat bo'lgani uchun bu YO'Q qilinadi (validator allaqachon
        // formatni tekshiradi, lekin bu yerda ham himoya bo'lishi kerak).
        byte[] provided;
        try
        {
            provided = Convert.FromHexString(hash);
        }
        catch (FormatException)
        {
            return false;
        }

        // DOIMIY vaqtda solishtirish — uzunliklar farq qilsa `FixedTimeEquals` `false` qaytaradi.
        return CryptographicOperations.FixedTimeEquals(computed, provided);
    }
}
