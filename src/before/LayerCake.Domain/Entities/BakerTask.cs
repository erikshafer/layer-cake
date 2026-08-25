using LayerCake.Domain.Common;

namespace LayerCake.Domain.Entities;

/// <summary>
/// A to-do item for the bakers, created when an order is placed. The
/// synchronously observable stand-in for "send the baker an email".
/// </summary>
public sealed class BakerTask : BaseAuditableEntity<Guid>
{
    public Guid OrderId { get; set; }

    public string Summary { get; set; } = string.Empty;
}
