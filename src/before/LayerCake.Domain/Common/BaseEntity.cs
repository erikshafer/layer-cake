namespace LayerCake.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// </summary>
public abstract class BaseEntity<TId>
{
    public TId Id { get; set; } = default!;
}
