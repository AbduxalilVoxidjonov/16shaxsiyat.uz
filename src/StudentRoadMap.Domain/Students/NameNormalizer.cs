using System.Text;

namespace StudentRoadMap.Domain.Students;

/// <summary>
/// F.I.SH. dublikatlarini aniqlash uchun ism-familiyani normalizatsiya qiladi:
/// katta harf, ortiqcha probel olib tashlanadi, tutuq belgilari (`'`, `’`, `ʻ`) birxillashtiriladi.
/// </summary>
public static class NameNormalizer
{
    public static string Normalize(string fullName)
    {
        ArgumentNullException.ThrowIfNull(fullName);

        var builder = new StringBuilder(fullName.Length);
        var previousWasSpace = false;

        foreach (var raw in fullName.Trim())
        {
            var ch = UnifyApostrophe(raw);

            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                }

                previousWasSpace = true;
                continue;
            }

            previousWasSpace = false;
            builder.Append(ch);
        }

        return builder.ToString().Trim().ToUpperInvariant();
    }

    private static char UnifyApostrophe(char ch) => ch switch
    {
        '\'' or '’' or 'ʻ' or 'ʼ' or '`' => '\'',
        _ => ch,
    };
}
