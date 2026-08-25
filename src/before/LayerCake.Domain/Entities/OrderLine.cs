namespace LayerCake.Domain.Entities;

/// <summary>
/// One line of an order. Name and UnitPrice are captured from the cake when
/// the order is placed, not joined live afterward.
/// </summary>
public sealed class OrderLine
{
    public Guid CakeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
