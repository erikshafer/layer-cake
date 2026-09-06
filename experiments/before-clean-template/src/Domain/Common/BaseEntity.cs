using System.ComponentModel.DataAnnotations.Schema;

namespace LayerCake.CleanTemplate.Domain.Common;

/// <summary>
/// Non-generic root so persistence interceptors can find every entity
/// regardless of key type. The key itself lives on <see cref="BaseEntity{TId}"/>.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public abstract class BaseEntity<TId> : BaseEntity
{
    public TId Id { get; set; } = default!;
}
