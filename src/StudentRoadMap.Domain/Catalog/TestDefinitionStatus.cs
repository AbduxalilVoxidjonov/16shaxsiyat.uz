namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// `TestDefinition.Status` (nashr holati) — qiymatlar `docs/05-database-schema.md` 3-bo'limidagi
/// `TestStatus` qatoriga mos (`1 Draft, 2 Published, 3 Archived`). Nom to'qnashuvining oldini olish
/// uchun bu yerda `TestDefinitionStatus` deb nomlangan — sessiya ichidagi bitta testning holati esa
/// <see cref="Assessments.TestStatus"/> (u boshqa qiymatlar to'plami: NotStarted/InProgress/Completed).
/// </summary>
public enum TestDefinitionStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3,
}
