namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>`VisibleQuestionResolver` kirishi uchun bitta bo'limning yengil nusxasi (`docs/18` §2.6).</summary>
public sealed record SectionSnapshot(Guid Id, string Code, int DisplayOrder, VisibilityRule? Visibility);
