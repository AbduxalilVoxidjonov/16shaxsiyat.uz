using System.Text;
using System.Text.RegularExpressions;

namespace StudentRoadMap.Application.Ai;

/// <summary>
/// Provayder xato matnini ("Tafsilot: ...") adminga ko'rsatishdan OLDIN xavfsiz holga keltiradi:
/// API kalitiga o'xshash satrlar (`AIza...`, `sk-...`, `sk-ant-...`, `Bearer ...`) `***` bilan
/// almashtiriladi, boshqaruv belgilari va ortiqcha bo'shliqlar yig'iladi, uzunlik cheklanadi.
/// <para>
/// Bu HIMOYA CHUQURLIGI: Infrastructure aniq kalitni (`AiHttpExecutor.Redact`) allaqachon
/// olib tashlaydi; bu yerda kalitni bilmasdan, shakli bo'yicha qo'shimcha tozalash qilinadi
/// (masalan provayder xabarida boshqa/eski kalit parchasi bo'lsa ham sizib chiqmasin).
/// </para>
/// </summary>
public static partial class AiErrorDetailSanitizer
{
    /// <summary>Adminga ko'rsatiladigan tafsilotning maksimal uzunligi.</summary>
    public const int MaxLength = 200;

    public static string? Sanitize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var scrubbed = KeyLikePattern().Replace(text, "***");

        var builder = new StringBuilder(scrubbed.Length);
        var previousWasSpace = false;
        foreach (var ch in scrubbed)
        {
            var isSpace = char.IsWhiteSpace(ch) || char.IsControl(ch);
            if (isSpace)
            {
                if (!previousWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                previousWasSpace = true;
                continue;
            }

            builder.Append(ch);
            previousWasSpace = false;
        }

        var result = builder.ToString().Trim();
        if (result.Length == 0)
        {
            return null;
        }

        return result.Length > MaxLength ? string.Concat(result.AsSpan(0, MaxLength), "…") : result;
    }

    /// <summary>
    /// Google (`AIza` + 35 belgi), OpenAI/Anthropic (`sk-...`, `sk-ant-...`, `sk-proj-...`) kalitlari
    /// va `Bearer <token>` / `key=<qiymat>` shakllari.
    /// </summary>
    [GeneratedRegex(@"AIza[0-9A-Za-z_\-]{10,}|sk-[0-9A-Za-z_\-]{8,}|(?i:bearer)\s+[0-9A-Za-z_\-\.]{8,}|(?i:key)=[0-9A-Za-z_\-]{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex KeyLikePattern();
}
