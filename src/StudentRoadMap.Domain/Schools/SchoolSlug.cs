using System.Text;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Schools;

/// <summary>
/// Maktab havolasi uchun URL-xavfsiz kod (`12-maktab-kokand`). O'zbek lotin/kirill
/// harflarini translit qiladi: `o'`→`o`, `g'`→`g`, `sh`→`sh`, `ch`→`ch`, bo'sh joy→`-`.
/// Natija faqat `[a-z0-9-]`, ketma-ket `-` siqiladi, boshi/oxiri `-` bo'lmaydi, uzunligi ≤ 80.
/// </summary>
public sealed class SchoolSlug : ValueObject
{
    public const int MaxLength = 80;

    public string Value { get; }

    private SchoolSlug(string value)
    {
        Value = value;
    }

    public static Result<SchoolSlug> Create(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Result.Failure<SchoolSlug>(new Error("SLUG_EMPTY", "Slug manba matni bo'sh bo'lishi mumkin emas."));
        }

        var slug = Slugify(source);

        if (slug.Length == 0)
        {
            return Result.Failure<SchoolSlug>(new Error("SLUG_EMPTY", "Berilgan matndan yaroqli slug hosil bo'lmadi."));
        }

        return Result.Success(new SchoolSlug(slug));
    }

    /// <summary>Allaqachon to'g'ri formatdagi (masalan, DB'dan o'qilgan) qiymatdan qayta yaratish.</summary>
    public static SchoolSlug FromExisting(string value) => new(value);

    private static string Slugify(string source)
    {
        // 1-bosqich: kirill harflarni lotin ekvivalentiga o'giramiz
        // (apostrofli harflar — ў→o', ғ→g' — keyingi bosqichda yana translit qilinadi).
        var latin = TransliterateCyrillic(source);

        // 2-bosqich: apostrofli o'zbek digraflarini soddalashtiramiz.
        var normalized = new StringBuilder(latin.Length);
        for (var i = 0; i < latin.Length; i++)
        {
            var current = char.ToLowerInvariant(latin[i]);
            var isApostropheLetter = current is 'o' or 'g';
            var nextIsApostrophe = i + 1 < latin.Length && IsApostrophe(latin[i + 1]);

            if (isApostropheLetter && nextIsApostrophe)
            {
                normalized.Append(current); // o' → o, g' → g
                i++; // apostrofni tashlab ketamiz
                continue;
            }

            if (IsApostrophe(current))
            {
                continue; // yolg'iz tutuq belgisi — olib tashlanadi
            }

            normalized.Append(current);
        }

        // 3-bosqich: ruxsat etilmagan belgilarni '-' ga almashtirib, faqat [a-z0-9-] qoldiramiz.
        var result = new StringBuilder(normalized.Length);
        var lastWasDash = false;
        foreach (var ch in normalized.ToString())
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                result.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash && result.Length > 0)
            {
                result.Append('-');
                lastWasDash = true;
            }
        }

        // Oxiridagi '-' ni olib tashlaymiz (boshida hech qachon bo'lmaydi — yuqoridagi shart tufayli).
        while (result.Length > 0 && result[^1] == '-')
        {
            result.Length--;
        }

        var final = result.ToString();
        if (final.Length > MaxLength)
        {
            final = final[..MaxLength];
            final = final.TrimEnd('-');
        }

        return final;
    }

    private static bool IsApostrophe(char ch) => ch is '\'' or '’' or 'ʼ' or '`';

    private static string TransliterateCyrillic(string source)
    {
        var builder = new StringBuilder(source.Length * 2);
        foreach (var ch in source)
        {
            builder.Append(CyrillicMap.TryGetValue(ch, out var mapped) ? mapped : ch.ToString());
        }

        return builder.ToString();
    }

    private static readonly Dictionary<char, string> CyrillicMap = new()
    {
        ['а'] = "a", ['А'] = "a",
        ['б'] = "b", ['Б'] = "b",
        ['в'] = "v", ['В'] = "v",
        ['г'] = "g", ['Г'] = "g",
        ['д'] = "d", ['Д'] = "d",
        ['е'] = "e", ['Е'] = "e",
        ['ё'] = "yo", ['Ё'] = "yo",
        ['ж'] = "j", ['Ж'] = "j",
        ['з'] = "z", ['З'] = "z",
        ['и'] = "i", ['И'] = "i",
        ['й'] = "y", ['Й'] = "y",
        ['к'] = "k", ['К'] = "k",
        ['л'] = "l", ['Л'] = "l",
        ['м'] = "m", ['М'] = "m",
        ['н'] = "n", ['Н'] = "n",
        ['о'] = "o", ['О'] = "o",
        ['п'] = "p", ['П'] = "p",
        ['р'] = "r", ['Р'] = "r",
        ['с'] = "s", ['С'] = "s",
        ['т'] = "t", ['Т'] = "t",
        ['у'] = "u", ['У'] = "u",
        ['ф'] = "f", ['Ф'] = "f",
        ['х'] = "x", ['Х'] = "x",
        ['ц'] = "ts", ['Ц'] = "ts",
        ['ч'] = "ch", ['Ч'] = "ch",
        ['ш'] = "sh", ['Ш'] = "sh",
        ['щ'] = "sht", ['Щ'] = "sht",
        ['ъ'] = "'", ['Ъ'] = "'",
        ['ы'] = "i", ['Ы'] = "i",
        ['ь'] = "", ['Ь'] = "",
        ['э'] = "e", ['Э'] = "e",
        ['ю'] = "yu", ['Ю'] = "yu",
        ['я'] = "ya", ['Я'] = "ya",
        ['ў'] = "o'", ['Ў'] = "o'",
        ['қ'] = "q", ['Қ'] = "q",
        ['ғ'] = "g'", ['Ғ'] = "g'",
        ['ҳ'] = "h", ['Ҳ'] = "h",
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
