namespace LayerCake.Slices.Orders;

/// <summary>
/// A to-do item for the bakers, created by the NotifyBaker handler. The
/// order's id IS the document identity, so a redelivered message upserts
/// instead of duplicating: exactly one task per order, by construction.
/// </summary>
public class BakerTask
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Summary { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
