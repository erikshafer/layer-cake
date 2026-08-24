using MediatR;

namespace LayerCake.Application.Coupons.Queries.ValidateCoupon;

/// <summary>
/// Asks "is this coupon any good right now?". Always answers with the
/// envelope; an unknown code is a normal answer, not an error.
/// </summary>
public sealed record ValidateCouponQuery(string Code) : IRequest<CouponValidationDto>;
