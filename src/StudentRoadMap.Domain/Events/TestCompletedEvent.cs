using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Events;

/// <summary>Bitta `AssessmentTest` yakunlanganda ko'tariladi — scoring handler tinglaydi.</summary>
public sealed record TestCompletedEvent(
    Guid AssessmentId,
    Guid AssessmentTestId,
    Guid TestDefinitionId,
    DateTimeOffset OccurredAt) : IDomainEvent;
