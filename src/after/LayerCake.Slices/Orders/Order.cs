using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayerCake.Slices.Orders;

/// <summary>
/// A customer order. Lines snapshot the cake's name and price at placement
/// time, and the computed totals are stored with the row, so reads return
/// exactly what placement calculated. Serialized straight onto the wire,
/// which is why CouponCode omits itself when null.
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

public class OrderTable : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Subtotal).HasPrecision(10, 2);
        builder.Property(o => o.Discount).HasPrecision(10, 2);
        builder.Property(o => o.Total).HasPrecision(10, 2);
        builder.Property(o => o.CouponCode).HasMaxLength(50);

        // The lines belong to the order and are never queried on their own, so
        // they stay one jsonb column instead of a child table with a foreign
        // key. ToJson() is EF Core's way of saying what the document store
        // said by default.
        builder.OwnsMany(o => o.Lines, lines =>
        {
            lines.ToJson();
            lines.Property(l => l.UnitPrice).HasPrecision(10, 2);
            lines.Property(l => l.LineTotal).HasPrecision(10, 2);
        });
    }
}
