using System.Text.Json.Serialization;

namespace LayerCake.Application.Orders;

/// <summary>
/// The shape the Application layer hands to the presentation layer for both
/// the placement response and the read-back. CouponCode stays null when no
/// coupon was sent, and null means absent on the wire.
/// </summary>
public sealed record OrderDto
{
    public Guid Id { get; init; }

    public List<OrderLineDto> Lines { get; init; } = [];

    public decimal Subtotal { get; init; }

    public decimal Discount { get; init; }

    public decimal Total { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CouponCode { get; init; }

    public DateTimeOffset PlacedAt { get; init; }
}
