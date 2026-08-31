namespace StudentRoadMap.Domain.Common;

/// <summary>
/// Agregat ildizi — domen hodisalarini to'plab boradi, tashqariga faqat o'qish uchun beradi.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
