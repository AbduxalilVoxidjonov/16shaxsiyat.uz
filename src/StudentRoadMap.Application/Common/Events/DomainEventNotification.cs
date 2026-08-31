using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Common.Events;

/// <summary>
/// `Domain.IDomainEvent` ni MediatR `INotification` ga o'raydi — `Domain` MediatR'ga bog'liq
/// bo'lmasligi kerak (`CLAUDE.md` 2-qoida), shu sabab o'rash Application qatlamida.
/// `Infrastructure/Persistence/AppDbContext.SaveChangesAsync` dan keyin publish qilinadi.
/// </summary>
public sealed class DomainEventNotification<TDomainEvent>(TDomainEvent domainEvent) : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; } = domainEvent;
}
