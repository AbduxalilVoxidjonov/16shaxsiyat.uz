namespace StudentRoadMap.Infrastructure.Persistence.Seeding;

/// <summary>`SeedData/type-catalog.json` dagi bitta yozuv (`docs/05` `type_catalog` jadvali).</summary>
public sealed record TypeCatalogSeedDto
{
    public string Code { get; init; } = string.Empty;

    public string NameUz { get; init; } = string.Empty;

    public string ShortDescriptionUz { get; init; } = string.Empty;

    public string LongDescriptionUz { get; init; } = string.Empty;

    public IReadOnlyList<string> Strengths { get; init; } = [];

    public IReadOnlyList<string> GrowthAreas { get; init; } = [];

    public IReadOnlyList<string> CareerHints { get; init; } = [];
}
