namespace StudentRoadMap.Domain.Catalog;

/// <summary>Anketa turi — qiymatlar `docs/05-database-schema.md` 3-bo'limiga mos.</summary>
public enum TestKind
{
    /// <summary>Ilmiy metodika (seed'dan keladi, `IsSystem = true`).</summary>
    Standard = 1,

    /// <summary>Superadmin yaratgan anketa.</summary>
    Custom = 2,
}
