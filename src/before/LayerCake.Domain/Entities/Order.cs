using LayerCake.Domain.Common;

namespace LayerCake.Domain.Entities;

/// <summary>
/// A customer order. Lines snapshot the cake's name and price at placement
/// time, and the computed totals are stored with the order so reads never
/// recompute (or drift from) the money math.
/// </summary>
public sealed class Order : BaseAuditableEntity<Guid>
{
    public List<OrderLine> Lines { get; set; } = [];

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Total { get; set; }

    public string? CouponCode { get; set; }

    public DateTimeOffset PlacedAt { get; set; }
}
