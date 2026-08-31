namespace StudentRoadMap.Application.Seeding;

/// <summary>
/// Seed JSON fayli noto'g'ri formatlangan yoki majburiy maydon yo'q bo'lganda otiladi
/// (`prompts/04-katalog-va-seed-infratuzilma.md`: "noto'g'ri JSON aniq xato beradi").
/// </summary>
public sealed class SeedDataFormatException : Exception
{
    /// <summary>Xato qaysi fayl/manbadan chiqqani — log va xabarda foydalanuvchiga ko'rsatiladi.</summary>
    public string SourceName { get; }

    public SeedDataFormatException(string sourceName, string message)
        : base($"[{sourceName}] {message}")
    {
        SourceName = sourceName;
    }

    public SeedDataFormatException(string sourceName, string message, Exception innerException)
        : base($"[{sourceName}] {message}", innerException)
    {
        SourceName = sourceName;
    }
}
