using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Enums;
using MediatR;

namespace LayerCake.Application.Coupons.Queries.ValidateCoupon;

public sealed class ValidateCouponQueryHandler : IRequestHandler<ValidateCouponQuery, CouponValidationDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly ICouponValidationService _couponValidationService;

    public ValidateCouponQueryHandler(
        ICouponRepository couponRepository,
        ICouponValidationService couponValidationService)
    {
        _couponRepository = couponRepository;
        _couponValidationService = couponValidationService;
    }

    public async Task<CouponValidationDto> Handle(ValidateCouponQuery request, CancellationToken cancellationToken)
    {
        // One normalization at the edge: lookup is case-insensitive and the
        // envelope echoes the canonical uppercase form.
        var canonicalCode = request.Code.ToUpperInvariant();

        var coupon = await _couponRepository.GetByCodeAsync(canonicalCode, cancellationToken);
        var status = _couponValidationService.Validate(coupon);

        return new CouponValidationDto
        {
            Code = canonicalCode,
            Status = ToWireStatus(status),
            PercentOff = status == CouponStatus.Valid ? coupon!.PercentOff : null
        };
    }

    private static string ToWireStatus(CouponStatus status)
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
