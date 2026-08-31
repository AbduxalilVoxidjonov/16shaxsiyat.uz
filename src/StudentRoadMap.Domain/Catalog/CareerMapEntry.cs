using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// RIASEC (Holland) kodi bo'yicha kasb yo'nalishlari xaritasi (`docs/04` 2.7-bo'lim,
/// `docs/05` `career_map`).
/// </summary>
public sealed class CareerMapEntry : Entity
{
    public string HollandCode { get; private set; } = null!;

    public string FieldNameUz { get; private set; } = null!;

    public string? DescriptionUz { get; private set; }

    public IReadOnlyList<string> ExampleProfessions { get; private set; } = [];

    public int RelevanceOrder { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private CareerMapEntry()
    {
    }

    private CareerMapEntry(Guid id, string hollandCode, string fieldNameUz, string? descriptionUz, IReadOnlyList<string> exampleProfessions, int relevanceOrder)
        : base(id)
    {
        HollandCode = hollandCode;
        FieldNameUz = fieldNameUz;
        DescriptionUz = descriptionUz;
        ExampleProfessions = exampleProfessions;
        RelevanceOrder = relevanceOrder;
    }

    public static CareerMapEntry Create(
        Guid id,
        string hollandCode,
        string fieldNameUz,
        int relevanceOrder = 1,
        string? descriptionUz = null,
        IReadOnlyList<string>? exampleProfessions = null)
    {
        if (string.IsNullOrWhiteSpace(hollandCode) || hollandCode.Length > 2)
        {
            throw new ArgumentException("Holland kodi 1-2 harfdan iborat bo'lishi kerak.", nameof(hollandCode));
        }

        if (string.IsNullOrWhiteSpace(fieldNameUz))
        {
            throw new ArgumentException("Yo'nalish nomi bo'sh bo'lishi mumkin emas.", nameof(fieldNameUz));
        }

        return new CareerMapEntry(id, hollandCode, fieldNameUz, descriptionUz, exampleProfessions ?? [], relevanceOrder);
    }
}
