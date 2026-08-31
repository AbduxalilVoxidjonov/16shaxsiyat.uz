namespace StudentRoadMap.Domain.Identity;

/// <summary>Admin roli — qiymatlar `docs/05-database-schema.md` 3-bo'limiga mos.</summary>
public enum AdminRole
{
    SuperAdmin = 1,

    /// <summary>v2 uchun rejalashtirilgan — MVP'da ishlatilmaydi.</summary>
    SchoolAdmin = 2,

    /// <summary>v2 uchun rejalashtirilgan — MVP'da ishlatilmaydi.</summary>
    Psychologist = 3,
}
