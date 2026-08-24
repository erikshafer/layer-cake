using LayerCake.Domain.Common;

namespace LayerCake.Domain.Entities;

/// <summary>
/// A discount coupon. Codes are stored canonically uppercase, and validity
/// is pure date mechanics: an active window between StartsAt and ExpiresAt.
/// </summary>
public sealed class Coupon : BaseAuditableEntity<Guid>
{
    public string Code { get; set; } = string.Empty;

    public int PercentOff { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
