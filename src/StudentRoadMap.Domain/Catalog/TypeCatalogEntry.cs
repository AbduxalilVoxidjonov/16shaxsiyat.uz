using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Shaxsiyat tipi katalogi (masalan, `INTJ` — "Strateg"). `Code` — jadval kaliti
/// (`docs/04` 2.7-bo'lim, `docs/05` `type_catalog`).
/// </summary>
public sealed class TypeCatalogEntry : ValueObject
{
    public string Code { get; }

    public string NameUz { get; }

    public string ShortDescriptionUz { get; }

    public string LongDescriptionUz { get; }

    public IReadOnlyList<string> Strengths { get; }

    public IReadOnlyList<string> GrowthAreas { get; }

    public IReadOnlyList<string> CareerHints { get; }

    private TypeCatalogEntry(
        string code,
        string nameUz,
        string shortDescriptionUz,
        string longDescriptionUz,
        IReadOnlyList<string> strengths,
        IReadOnlyList<string> growthAreas,
        IReadOnlyList<string> careerHints)
    {
        Code = code;
        NameUz = nameUz;
        ShortDescriptionUz = shortDescriptionUz;
        LongDescriptionUz = longDescriptionUz;
        Strengths = strengths;
        GrowthAreas = growthAreas;
        CareerHints = careerHints;
    }

    public static TypeCatalogEntry Create(
        string code,
        string nameUz,
        string shortDescriptionUz,
        string longDescriptionUz,
        IReadOnlyList<string>? strengths = null,
        IReadOnlyList<string>? growthAreas = null,
        IReadOnlyList<string>? careerHints = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Tip kodi bo'sh bo'lishi mumkin emas.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(nameUz))
        {
            throw new ArgumentException("Tip nomi bo'sh bo'lishi mumkin emas.", nameof(nameUz));
        }

        return new TypeCatalogEntry(
            code,
            nameUz,
            shortDescriptionUz,
            longDescriptionUz,
            strengths ?? [],
            growthAreas ?? [],
            careerHints ?? []);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return NameUz;
        yield return ShortDescriptionUz;
        yield return LongDescriptionUz;
    }
}
