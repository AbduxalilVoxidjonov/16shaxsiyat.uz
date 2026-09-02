namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// `AssessmentProgram.Status` (nashr holati) — `prompts/34` A-band. Nom to'qnashuvining oldini
/// olish uchun `TestDefinitionStatus`ga o'xshash, lekin alohida enum (dastur va anketa nashr
/// holatlari mustaqil).
/// </summary>
public enum ProgramStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3,
}
