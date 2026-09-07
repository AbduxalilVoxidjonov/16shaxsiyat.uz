using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Events;

/// <summary>Maktab kodi (`School.EntryCode`) qayta generatsiya qilinganda ko'tariladi — audit uchun (`SchoolLinkRegeneratedEvent` bilan bir xil naqsh).</summary>
public sealed record SchoolEntryCodeRegeneratedEvent(
    Guid SchoolId,
    DateTimeOffset OccurredAt) : IDomainEvent;
