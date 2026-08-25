using System.Text.Json.Serialization;

namespace LayerCake.Slices.Orders;

/// <summary>
/// A customer order. Lines snapshot the cake's name and price at placement
/// time, and the computed totals are stored with the document, so reads
/// return exactly what placement calculated. Serialized straight onto the
/// wire, which is why CouponCode omits itself when null.
/// </summary>
public class Order
{
    public Guid Id { get; set; }

    public List<OrderLine> Lines { get; set; } = [];

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Total { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CouponCode { get; set; }

    public DateTimeOffset PlacedAt { get; set; }
}

/// <summary>
/// One line of an order: the snapshot taken at placement, never a live
/// join back to the cake.
/// </summary>
public class OrderLine
{
    public Guid CakeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
