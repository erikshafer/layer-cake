using LayerCake.Slices.Cakes;
using LayerCake.Slices.Coupons;
using LayerCake.Slices.Orders;
using Shouldly;
using Wolverine.Http;
using Xunit;

namespace LayerCake.Slices.Marten.Tests;

/// <summary>
/// The A-Frame payoff: Decide and Validate are static functions of their
/// inputs, so the whole pricing model and the guard chain are tested here
/// with no host, no Marten, and no mocks. Compare with what it takes to
/// exercise the before twin's PlaceOrderCommandHandler.
/// </summary>
public class PlaceOrderTests
{
    private static readonly Cake Stout = new()
    {
        Id = Guid.NewGuid(), Name = "Chocolate Stout", Description = "Six layers, no mercy", Price = 34.00m
    };

    private static readonly Cake Lemon = new()
    {
        Id = Guid.NewGuid(), Name = "Lemon Chiffon", Description = "Light, tart", Price = 28.00m
    };

    private static readonly DateTimeOffset PlacedAt = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);

    private static Coupon TenPercent(DateTimeOffset now) => new()
    {
        Code = "BDAY10", PercentOff = 10, StartsAt = now.AddYears(-1), ExpiresAt = now.AddYears(1)
    };

    [Fact]
    public void decide_snapshots_name_and_price_and_sums_the_lines()
    {
        var lines = new List<PlaceOrderLine> { new(Stout.Id, 2), new(Lemon.Id, 1) };

        var order = PlaceOrderEndpoint.Decide(lines, [Stout, Lemon], coupon: null, PlacedAt);

        order.Id.ShouldNotBe(Guid.Empty);
        order.PlacedAt.ShouldBe(PlacedAt);
        order.Lines.Count.ShouldBe(2);

        var stoutLine = order.Lines.Single(l => l.CakeId == Stout.Id);
        stoutLine.Name.ShouldBe("Chocolate Stout");
        stoutLine.UnitPrice.ShouldBe(34.00m);
        stoutLine.Quantity.ShouldBe(2);
        stoutLine.LineTotal.ShouldBe(68.00m);

        order.Subtotal.ShouldBe(96.00m);
        order.Discount.ShouldBe(0m);
        order.Total.ShouldBe(96.00m);
        order.CouponCode.ShouldBeNull();
    }

    [Fact]
    public void decide_applies_the_coupon_percentage_and_records_its_code()
    {
        var lines = new List<PlaceOrderLine> { new(Stout.Id, 2) };

        var order = PlaceOrderEndpoint.Decide(lines, [Stout], TenPercent(PlacedAt), PlacedAt);

        order.Subtotal.ShouldBe(68.00m);
        order.Discount.ShouldBe(6.80m);
        order.Total.ShouldBe(61.20m);
        order.CouponCode.ShouldBe("BDAY10");
    }

    [Fact]
    public void decide_rounds_a_half_cent_discount_to_even()
    {
        // 24.05 at 10% is 2.405; banker's rounding lands on 2.40, not 2.41.
        var oddPriced = new Cake { Id = Guid.NewGuid(), Name = "Odd", Price = 24.05m };
        var lines = new List<PlaceOrderLine> { new(oddPriced.Id, 1) };

        var order = PlaceOrderEndpoint.Decide(lines, [oddPriced], TenPercent(PlacedAt), PlacedAt);

        order.Discount.ShouldBe(2.40m);
        order.Total.ShouldBe(21.65m);
    }

    [Fact]
    public void validate_rejects_empty_lines_with_400()
    {
        var command = new PlaceOrder([], CouponCode: null);

        var problem = PlaceOrderEndpoint.Validate(command, new PlaceOrderData([], null));

        problem.Status.ShouldBe(400);
        problem.Detail.ShouldNotBeNull().ShouldContain("Lines");
    }

    [Fact]
    public void validate_rejects_a_zero_quantity_with_400()
    {
        var command = new PlaceOrder([new PlaceOrderLine(Stout.Id, 0)], CouponCode: null);

        var problem = PlaceOrderEndpoint.Validate(command, new PlaceOrderData([Stout], null));

        problem.Status.ShouldBe(400);
        problem.Detail.ShouldNotBeNull().ShouldContain("Quantity");
    }

    [Fact]
    public void validate_lists_every_unknown_cake_id_with_422()
    {
        var missing = Guid.NewGuid();
        var command = new PlaceOrder([new PlaceOrderLine(Stout.Id, 1), new PlaceOrderLine(missing, 1)], CouponCode: null);

        // The loader only found the stout; the second id is unknown.
        var problem = PlaceOrderEndpoint.Validate(command, new PlaceOrderData([Stout], null));

        problem.Status.ShouldBe(422);
        problem.Detail.ShouldNotBeNull().ShouldContain(missing.ToString());
        problem.Detail.ShouldNotBeNull().ShouldNotContain(Stout.Id.ToString());
    }

    [Fact]
    public void validate_rejects_an_expired_coupon_with_422_naming_the_status()
    {
        var now = DateTimeOffset.UtcNow;
        var expired = new Coupon
        {
            Code = "SUMMER25", PercentOff = 25, StartsAt = now.AddYears(-1), ExpiresAt = now.AddDays(-1)
        };
        var command = new PlaceOrder([new PlaceOrderLine(Stout.Id, 1)], "summer25");

        var problem = PlaceOrderEndpoint.Validate(command, new PlaceOrderData([Stout], expired));

        problem.Status.ShouldBe(422);
        problem.Detail.ShouldNotBeNull().ShouldContain("SUMMER25");
        problem.Detail.ShouldNotBeNull().ShouldContain("expired");
    }

    [Fact]
    public void validate_treats_an_unknown_coupon_as_invalid_with_422()
    {
        var command = new PlaceOrder([new PlaceOrderLine(Stout.Id, 1)], "NOPE");

        // The loader found no coupon for the code.
        var problem = PlaceOrderEndpoint.Validate(command, new PlaceOrderData([Stout], null));

        problem.Status.ShouldBe(422);
        problem.Detail.ShouldNotBeNull().ShouldContain("invalid");
    }

    [Fact]
    public void validate_lets_a_well_formed_order_through()
    {
        var command = new PlaceOrder([new PlaceOrderLine(Stout.Id, 2)], "BDAY10");

        var problem = PlaceOrderEndpoint.Validate(command, new PlaceOrderData([Stout], TenPercent(DateTimeOffset.UtcNow)));

        // Wolverine checks this by reference, so the test does too.
        ReferenceEquals(problem, WolverineContinue.NoProblems).ShouldBeTrue();
    }
}
