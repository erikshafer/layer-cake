using System.Text.Json.Serialization;

namespace LayerCake.Slices.Coupons;

/// <summary>
/// The four possible answers to "is this coupon any good right now?".
/// Serializes straight to the contract's camelCase strings on the wire.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CouponStatus>))]
public enum CouponStatus
{
    [JsonStringEnumMemberName("invalid")]
    Invalid,

    [JsonStringEnumMemberName("notYetActive")]
    NotYetActive,

    [JsonStringEnumMemberName("expired")]
    Expired,

    [JsonStringEnumMemberName("valid")]
    Valid
}

/// <summary>
/// THE coupon rule: one pure function, two call sites (the ValidateCoupon
/// endpoint here, PlaceOrder's guard chain in slice 003). Clock-parameterized
/// so tests never mock time.
/// </summary>
public static class CouponValidation
{
    // Railway Oriented Programming without the ceremony: three ordered
    // checks, first failure wins. exists -> active window -> expiry -> valid.
    public static CouponStatus Evaluate(Coupon? coupon, DateTimeOffset now)
    {
        if (coupon is null)
        {
            return CouponStatus.Invalid;
        }

        if (coupon.StartsAt > now)
        {
            return CouponStatus.NotYetActive;
        }

        if (coupon.ExpiresAt < now)
        {
            return CouponStatus.Expired;
        }

        return CouponStatus.Valid;
    }
}
