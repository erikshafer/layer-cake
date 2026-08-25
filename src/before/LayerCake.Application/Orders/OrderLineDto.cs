namespace LayerCake.Application.Orders;

/// <summary>
/// One order line as the presentation layer sees it: the snapshot taken at
/// placement time, never a live join back to the cake.
/// </summary>
public sealed record OrderLineDto
{
    public Guid CakeId { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }

    public decimal LineTotal { get; init; }
}
