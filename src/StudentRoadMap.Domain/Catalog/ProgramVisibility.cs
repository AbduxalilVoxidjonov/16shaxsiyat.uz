namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// `AssessmentProgram.Visibility` — `docs/06-arxitektura.md` 8-bo'lim (2026-09-02 qaror),
/// `prompts/34` A-band. `Public` — barcha maktabda ko'rinadi. `Assigned` — faqat
/// `SchoolProgram` orqali aniq biriktirilgan maktablarda.
/// </summary>
public enum ProgramVisibility
{
    Public = 1,
    Assigned = 2,
}
