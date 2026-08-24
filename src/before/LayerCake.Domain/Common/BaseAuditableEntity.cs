namespace LayerCake.Domain.Common;

/// <summary>
/// Base class for entities that track creation and modification timestamps.
/// </summary>
public abstract class BaseAuditableEntity<TId> : BaseEntity<TId>
{
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }
}
