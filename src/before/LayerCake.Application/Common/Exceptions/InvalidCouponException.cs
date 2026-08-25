namespace LayerCake.Application.Common.Exceptions;

/// <summary>
/// Thrown when an order's coupon does not evaluate to valid. Mapped to a
/// 422 in the WebApi layer; the message carries the failing wire status
/// (invalid, notYetActive, or expired). Checkout is authoritative: a bad
/// coupon fails the order, never silently drops.
/// </summary>
public sealed class InvalidCouponException : Exception
{
    public InvalidCouponException(string code, string wireStatus)
        : base($"Coupon \"{code}\" is not valid: {wireStatus}.")
    {
        Code = code;
        WireStatus = wireStatus;
    }

    public string Code { get; }

    public string WireStatus { get; }
}
