namespace StudentRoadMap.Domain.Catalog;

/// <summary>Savol turi — qiymatlar `docs/05-database-schema.md` 3-bo'limiga mos.</summary>
public enum QuestionType
{
    Likert5 = 1,
    Likert7 = 2,
    Binary = 3,
    SingleChoice = 4,
    ForcedChoice = 5,
}
