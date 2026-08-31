using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Events;

/// <summary>AI tahlil muvaffaqiyatli yakunlanganda ko'tariladi — `StudentSnapshot` yangilanadi.</summary>
public sealed record AiAnalysisSucceededEvent(
    Guid AssessmentId,
    Guid AiAnalysisId,
    DateTimeOffset OccurredAt) : IDomainEvent;
