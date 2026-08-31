using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Events;

/// <summary>Maktab havolasi (`AccessToken`) qayta generatsiya qilinganda ko'tariladi — audit uchun.</summary>
public sealed record SchoolLinkRegeneratedEvent(
    Guid SchoolId,
    DateTimeOffset OccurredAt) : IDomainEvent;
