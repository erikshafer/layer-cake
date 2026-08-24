using LayerCake.Domain.Entities;
using LayerCake.Domain.Enums;

namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// THE coupon rule, behind an interface. Two consumers: the ValidateCoupon
/// query handler (slice 002) and PlaceOrder's guard chain (slice 003).
/// </summary>
public interface ICouponValidationService
{
    CouponStatus Validate(Coupon? coupon);
}
