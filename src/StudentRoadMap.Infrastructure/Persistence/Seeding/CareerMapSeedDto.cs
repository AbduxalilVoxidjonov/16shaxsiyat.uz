namespace StudentRoadMap.Infrastructure.Persistence.Seeding;

/// <summary>`SeedData/career-map.json` dagi bitta yozuv (`docs/05` `career_map` jadvali).</summary>
public sealed record CareerMapSeedDto
{
    public string HollandCode { get; init; } = string.Empty;

    public string FieldNameUz { get; init; } = string.Empty;

    public string? DescriptionUz { get; init; }

    public IReadOnlyList<string> ExampleProfessions { get; init; } = [];

    public int RelevanceOrder { get; init; } = 1;
}
