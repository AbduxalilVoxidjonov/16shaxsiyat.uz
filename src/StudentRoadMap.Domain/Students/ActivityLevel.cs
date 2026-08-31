namespace StudentRoadMap.Domain.Students;

/// <summary>
/// O'quvchi aktivlik darajasi (snapshot uchun) — qiymatlar
/// `docs/05-database-schema.md` 3-bo'limiga aynan mos.
/// </summary>
public enum ActivityLevel
{
    Passive = 1,
    LowActive = 2,
    Moderate = 3,
    Active = 4,
    HighlyActive = 5,
}
