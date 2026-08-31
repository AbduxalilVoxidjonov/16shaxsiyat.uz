using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Holland (RIASEC) kodi — `docs/04-domain-model.md` §4: "3 harf, `RIASEC` alifbosidan".
/// Matnda qisqalik uchun bitta harfli mnemonika ishlatiladi (`docs/03` §4.1: "matnda qisqalik
/// uchun `R-I-A-S-E-C` harflari"), bazadagi `Scale` kodlari (`R,I,ART,SOC,ENT,CONV`) emas.
/// </summary>
public sealed class HollandCode : ValueObject
{
    /// <summary>Ruxsat etilgan harflar, olti burchak (hexagon) tartibida — konsistentlik hisobida ham ishlatiladi.</summary>
    public static readonly char[] HexagonOrder = ['R', 'I', 'A', 'S', 'E', 'C'];

    public string Code { get; }

    private HollandCode(string code)
    {
        Code = code;
    }

    /// <summary>1..3 ta noyob, `HexagonOrder`ga tegishli harflardan Holland kodini yaratadi.</summary>
    public static HollandCode Create(IReadOnlyList<char> letters)
    {
        if (letters.Count is < 1 or > 3)
        {
            throw new DomainException("INVALID_HOLLAND_CODE", "Holland kodi 1..3 harfdan iborat bo'lishi kerak.");
        }

        var upper = letters.Select(char.ToUpperInvariant).ToList();

        foreach (var letter in upper)
        {
            if (Array.IndexOf(HexagonOrder, letter) < 0)
            {
                throw new DomainException(
                    "INVALID_HOLLAND_CODE",
                    $"Holland kodi faqat {string.Join(',', HexagonOrder)} harflaridan iborat bo'lishi mumkin, berilgan: '{letter}'.");
            }
        }

        if (upper.Distinct().Count() != upper.Count)
        {
            throw new DomainException("INVALID_HOLLAND_CODE", "Holland kodida harflar takrorlanmasligi kerak.");
        }

        return new HollandCode(new string(upper.ToArray()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString() => Code;
}
