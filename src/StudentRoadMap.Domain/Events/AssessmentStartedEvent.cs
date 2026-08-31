using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Events;

/// <summary>Sessiya `Draft` holatida yaratilganda ko'tariladi (`docs/04` 3-bo'lim).</summary>
public sealed record AssessmentStartedEvent(
    Guid AssessmentId,
    Guid StudentId,
    Guid SchoolId,
    DateTimeOffset OccurredAt) : IDomainEvent;
