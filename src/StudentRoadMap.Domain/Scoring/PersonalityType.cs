using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// 16 tipli shaxsiyat modelining 4 harfli kodi (masalan `INTJ`) — `docs/04-domain-model.md` §4:
/// "4 harf, validatsiya, `TypeCatalog` bilan bog'lanish". Har pozitsiya faqat `docs/03` §2.2
/// jadvalidagi ruxsat etilgan harfdan biri bo'lishi kerak.
/// </summary>
public sealed class PersonalityType : ValueObject
{
    private static readonly char[] EiLetters = ['E', 'I'];
    private static readonly char[] SnLetters = ['S', 'N'];
    private static readonly char[] TfLetters = ['T', 'F'];
    private static readonly char[] JpLetters = ['J', 'P'];

    public string Code { get; }

    private PersonalityType(string code)
    {
        Code = code;
    }

    /// <summary>4 ta harfdan (EI/SN/TF/JP tartibida) kodni yaratadi va validatsiya qiladi.</summary>
    public static PersonalityType Create(char ei, char sn, char tf, char jp)
    {
        ei = char.ToUpperInvariant(ei);
        sn = char.ToUpperInvariant(sn);
        tf = char.ToUpperInvariant(tf);
        jp = char.ToUpperInvariant(jp);

        ValidateLetter(ei, EiLetters, nameof(ei));
        ValidateLetter(sn, SnLetters, nameof(sn));
        ValidateLetter(tf, TfLetters, nameof(tf));
        ValidateLetter(jp, JpLetters, nameof(jp));

        return new PersonalityType(new string([ei, sn, tf, jp]));
    }

    /// <summary>Tayyor 4 harfli kod matnidan yaratadi (masalan seed/DB'dan o'qishda).</summary>
    public static PersonalityType FromCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 4)
        {
            throw new DomainException("INVALID_PERSONALITY_TYPE", "Shaxsiyat tipi kodi aynan 4 harfdan iborat bo'lishi kerak.");
        }

        return Create(code[0], code[1], code[2], code[3]);
    }

    private static void ValidateLetter(char letter, char[] allowed, string axisName)
    {
        if (Array.IndexOf(allowed, letter) < 0)
        {
            throw new DomainException(
                "INVALID_PERSONALITY_TYPE",
                $"'{axisName}' o'qi uchun harf faqat {string.Join('/', allowed)} bo'lishi mumkin, berilgan: '{letter}'.");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString() => Code;
}
