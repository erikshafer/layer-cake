using LayerCake.Domain.Common;

namespace LayerCake.Domain.Entities;

/// <summary>
/// A layer cake offered by the bakery.
/// </summary>
public sealed class Cake : BaseAuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTimeOffset PublishedAt { get; set; }
}
