namespace LayerCake.Application.Orders;

/// <summary>
/// A card payment as the presentation layer sees it: the vendor's
/// authorization id and the payment status, never the card.
/// </summary>
public sealed record PaymentDto
{
    public Guid AuthorizationId { get; init; }

    public string Status { get; init; } = string.Empty;
}
