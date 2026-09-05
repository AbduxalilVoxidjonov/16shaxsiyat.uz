using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>
/// Telegram Login Widget javobini SINOV uchun quradi va imzolaydi.
///
/// Imzo bu yerda MUSTAQIL (ishlab chiqarish kodidan nusxa OLMASDAN, Telegram hujjatidagi
/// ta'rifga qarab) hisoblanadi — shu sabab test `TelegramLoginVerifier` ni haqiqatan
/// SPETSIFIKATSIYAGA qarshi tekshiradi. Agar ikkalasi bir xil yordamchidan foydalansa,
/// algoritmdagi xato ikkala tomonda ham bir xil bo'lib, test yashil qolib ketardi.
/// </summary>
internal static class TelegramLoginTestSupport
{
    /// <summary>Imzolangan so'rov tanasi (`POST /api/auth/telegram` uchun tayyor obyekt).</summary>
    public static Dictionary<string, object> BuildSignedPayload(
        long id,
        string botToken = TelegramApiTestFactory.BotToken,
        DateTimeOffset? authDate = null,
        string? firstName = "Ali",
        string? lastName = null,
        string? username = null,
        string? photoUrl = null)
    {
        var authDateSeconds = (authDate ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();

        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = id.ToString(CultureInfo.InvariantCulture),
            ["auth_date"] = authDateSeconds.ToString(CultureInfo.InvariantCulture),
        };

        AddIfPresent(fields, "first_name", firstName);
        AddIfPresent(fields, "last_name", lastName);
        AddIfPresent(fields, "username", username);
        AddIfPresent(fields, "photo_url", photoUrl);

        var payload = fields.ToDictionary(pair => pair.Key, pair => (object)pair.Value, StringComparer.Ordinal);

        // `id`/`auth_date` JSON'da RAQAM sifatida ketadi (Telegram widget ham shunday beradi),
        // imzo esa ularning satr ko'rinishidan hisoblanadi.
        payload["id"] = id;
        payload["auth_date"] = authDateSeconds;
        payload["hash"] = ComputeHash(fields, botToken);

        return payload;
    }

    public static string ComputeHash(IReadOnlyDictionary<string, string> fields, string botToken)
    {
        var dataCheckString = string.Join(
            '\n',
            fields
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"));

        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(botToken));
        var hash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void AddIfPresent(IDictionary<string, string> fields, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            fields[key] = value;
        }
    }
}
