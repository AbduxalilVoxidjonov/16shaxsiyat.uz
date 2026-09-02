namespace StudentRoadMap.Application.Admin.Ai;

/// <summary>
/// API kalitini superadmin panelida ko'rsatish uchun maskalaydi — `docs/07-api-shartnoma.md` §3.5
/// namunasi: `AIza••••••7f2b` (birinchi/oxirgi 4 belgi ko'rinadi, orasi yashiriladi). Kalit
/// HECH QACHON to'liq qaytmaydi (`CLAUDE.md` qat'iy qoidasi).
/// </summary>
internal static class ApiKeyMasker
{
    private const int VisiblePrefixLength = 4;
    private const int VisibleSuffixLength = 4;
    private const string MaskMiddle = "••••••";

    public static string Mask(string plainTextApiKey)
    {
        if (string.IsNullOrEmpty(plainTextApiKey))
        {
            return string.Empty;
        }

        if (plainTextApiKey.Length <= VisiblePrefixLength + VisibleSuffixLength)
        {
            return MaskMiddle;
        }

        var prefix = plainTextApiKey[..VisiblePrefixLength];
        var suffix = plainTextApiKey[^VisibleSuffixLength..];
        return $"{prefix}{MaskMiddle}{suffix}";
    }
}
