namespace LayerCake.Domain.Enums;

/// <summary>
/// The four possible answers to "is this coupon any good right now?".
/// Determined by date mechanics alone; there is no "exhausted".
/// </summary>
public enum CouponStatus
{
    Invalid,
    NotYetActive,
    Expired,
    Valid
}
