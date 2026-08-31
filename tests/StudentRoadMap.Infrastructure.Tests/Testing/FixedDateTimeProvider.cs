using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Tests.Testing;

/// <summary>Sinovlar uchun soxta `IDateTime` — `AppDbContext`/`DbSeeder` uchun barqaror vaqt.</summary>
internal sealed class FixedDateTimeProvider : IDateTime
{
    public FixedDateTimeProvider(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }
}
