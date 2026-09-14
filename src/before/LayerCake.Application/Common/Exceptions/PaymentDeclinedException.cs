namespace LayerCake.Application.Common.Exceptions;

/// <summary>
/// Thrown when the payment gateway declines an order's card. Mapped to a 402
/// in the WebApi layer; the message carries the gateway's reason.
/// </summary>
public sealed class PaymentDeclinedException : Exception
{
    public PaymentDeclinedException(string? reason)
        : base($"Card declined: {reason}.")
    {
        Reason = reason;
    }

    public string? Reason { get; }
}
