using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Maktabga aniq biriktirilgan dastur — `(SchoolId, ProgramId)` (`docs/06` 8-bo'lim,
/// `prompts/34` A3-band). Faqat `AssessmentProgram.Visibility = Assigned` dasturlar uchun
/// ma'noga ega — `Public` dasturlar bu jadvalsiz ham barcha maktabda ko'rinadi.
/// Mustaqil agregat (o'z-o'zicha `AssessmentProgram`/`School`ga tegishli emas) — EF'da
/// `IAppDbContext.SchoolPrograms` orqali to'g'ridan-to'g'ri so'raladi (`AssessmentTest`
/// naqshiga o'xshash).
/// </summary>
public sealed class SchoolProgram : Entity
{
    public Guid SchoolId { get; private set; }

    public Guid ProgramId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private SchoolProgram()
    {
    }

    private SchoolProgram(Guid id, Guid schoolId, Guid programId, DateTimeOffset now)
        : base(id)
    {
        SchoolId = schoolId;
        ProgramId = programId;
        CreatedAt = now;
    }

    public static SchoolProgram Create(Guid id, Guid schoolId, Guid programId, DateTimeOffset now) =>
        new(id, schoolId, programId, now);
}
