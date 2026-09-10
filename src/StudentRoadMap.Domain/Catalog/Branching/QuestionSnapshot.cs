namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>`VisibleQuestionResolver` kirishi uchun bitta savolning yengil nusxasi (`docs/18` §2.6).</summary>
public sealed record QuestionSnapshot(
    Guid Id,
    string Code,
    int DisplayOrder,
    bool IsActive,
    Guid? SectionId,
    VisibilityRule? Visibility);
