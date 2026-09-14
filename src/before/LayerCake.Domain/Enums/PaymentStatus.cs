namespace LayerCake.Domain.Enums;

/// <summary>
/// How an order is paid. AtPickup is the default and the zero value, so an
/// order placed without a card needs nothing set.
/// </summary>
public enum PaymentStatus
{
    AtPickup,
    Approved
}
