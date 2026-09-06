namespace LayerCake.CleanTemplate.Domain.Common;

/// <summary>
/// Non-generic root so <c>AuditableEntityInterceptor</c> can stamp every
/// auditable entity regardless of key type. The key itself lives on
/// <see cref="BaseAuditableEntity{TId}"/>.
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTimeOffset Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModified { get; set; }

    public string? LastModifiedBy { get; set; }
}

public abstract class BaseAuditableEntity<TId> : BaseAuditableEntity
{
    public TId Id { get; set; } = default!;
}
