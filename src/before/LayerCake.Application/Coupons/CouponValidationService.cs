using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Entities;
using LayerCake.Domain.Enums;

namespace LayerCake.Application.Coupons;

/// <summary>
/// Evaluates a coupon to its status. The date pipeline lives in one place so
/// both call sites (coupon validation and, later, order placement) agree.
/// </summary>
public sealed class CouponValidationService : ICouponValidationService
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public CouponValidationService(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public CouponStatus Validate(Coupon? coupon)
    {
        return Evaluate(coupon, _dateTimeProvider.UtcNow);
    }

    // The ordered checks, first failure wins:
    // exists -> active window -> expiry -> valid.
    private static CouponStatus Evaluate(Coupon? coupon, DateTimeOffset now)
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
