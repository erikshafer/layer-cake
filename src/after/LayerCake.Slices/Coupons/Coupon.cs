// [Identity] lives in the JasperFx namespace since the JasperFx 2.0 line
// (Marten 9); it is no longer Marten.Schema.IdentityAttribute.
using JasperFx;

namespace LayerCake.Slices.Coupons;

/// <summary>
/// A discount coupon. The uppercase code IS the document identity, so a
/// case-insensitive lookup is one ToUpperInvariant plus a straight load.
/// </summary>
public class Coupon
{
    [Identity]
    public string Code { get; set; } = string.Empty;

    public int PercentOff { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
