using LayerCake.Slices.Coupons;
using LayerCake.Slices.Features.Coupons;
using Shouldly;
using Xunit;

namespace LayerCake.Slices.Tests;

/// <summary>
/// THE coupon rule, exercised directly. The clock is a parameter, so no
/// fake time provider is needed to hit every branch.
/// </summary>
public class CouponValidationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);

    private static Coupon Window(DateTimeOffset startsAt, DateTimeOffset expiresAt) => new()
    {
        Code = "TEST", PercentOff = 10, StartsAt = startsAt, ExpiresAt = expiresAt
    };

    [Fact]
    public void a_missing_coupon_is_invalid()
        => CouponValidation.Evaluate(null, Now).ShouldBe(CouponStatus.Invalid);

    [Fact]
    public void a_coupon_that_starts_tomorrow_is_not_yet_active()
        => CouponValidation.Evaluate(Window(Now.AddDays(1), Now.AddYears(1)), Now)
            .ShouldBe(CouponStatus.NotYetActive);

    [Fact]
    public void a_coupon_that_expired_yesterday_is_expired()
        => CouponValidation.Evaluate(Window(Now.AddYears(-1), Now.AddDays(-1)), Now)
            .ShouldBe(CouponStatus.Expired);

    [Fact]
    public void a_coupon_inside_its_window_is_valid()
        => CouponValidation.Evaluate(Window(Now.AddYears(-1), Now.AddYears(1)), Now)
            .ShouldBe(CouponStatus.Valid);

    [Fact]
    public void the_window_boundaries_are_inclusive()
    {
        CouponValidation.Evaluate(Window(Now, Now.AddYears(1)), Now).ShouldBe(CouponStatus.Valid);
        CouponValidation.Evaluate(Window(Now.AddYears(-1), Now), Now).ShouldBe(CouponStatus.Valid);
    }
}
