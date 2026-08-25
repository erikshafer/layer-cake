using LayerCake.Domain.Enums;

namespace LayerCake.Application.Coupons;

/// <summary>
/// Maps the Domain <see cref="CouponStatus"/> enum to the contract's wire
/// strings. Shared by the ValidateCoupon envelope and PlaceOrder's coupon
/// guard so the two surfaces can never drift.
/// </summary>
public static class CouponStatusWire
{
    public static string ToWireString(this CouponStatus status)
    {
        return status switch
        {
            CouponStatus.Invalid => "invalid",
            CouponStatus.NotYetActive => "notYetActive",
            CouponStatus.Expired => "expired",
            CouponStatus.Valid => "valid",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}
