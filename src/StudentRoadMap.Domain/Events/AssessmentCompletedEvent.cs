using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Events;

/// <summary>Barcha testlar tugaganda ko'tariladi — ishonchlilik hisoblanadi, AI navbatga qo'yiladi.</summary>
public sealed record AssessmentCompletedEvent(
    Guid AssessmentId,
    Guid StudentId,
    Guid SchoolId,
    DateTimeOffset OccurredAt) : IDomainEvent;
