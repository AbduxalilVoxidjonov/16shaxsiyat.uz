using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Events;

/// <summary>3 urinishdan keyin ham AI javob bermaganda ko'tariladi — alert log uchun.</summary>
public sealed record AiAnalysisFailedEvent(
    Guid AssessmentId,
    Guid AiAnalysisId,
    string ErrorMessage,
    DateTimeOffset OccurredAt) : IDomainEvent;
