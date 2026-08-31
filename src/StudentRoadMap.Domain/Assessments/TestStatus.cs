namespace StudentRoadMap.Domain.Assessments;

/// <summary>
/// `AssessmentTest.Status` qiymatlari — `docs/05-database-schema.md` 3-bo'limiga mos.
/// (E'tibor: `docs/05` jadvalida xuddi shu nom bilan `TestDefinition.Status` uchun ham
/// boshqa qiymatlar to'plami keltirilgan — nom to'qnashuvi. Ular ikki xil enum: bu — sessiya
/// ichidagi bitta testning holati, katalogdagi anketa nashr holati esa <see cref="Catalog.TestDefinitionStatus"/>.)
/// </summary>
public enum TestStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
}
