using LayerCake.Slices.Orders;
using LayerCake.Slices.Payments;
using Shouldly;
using Wolverine.Http;
using Xunit;

namespace LayerCake.Slices.Tests;

/// <summary>
/// Guard four is a function of the Payment the rung above returned, so the
/// card outcomes are tested with a value and no Tendr, no HttpClient, no host.
/// </summary>
public class PaymentGuardTests
{
    [Fact]
    public void validate_lets_an_order_without_a_card_through()
    {
        var problem = PlaceOrderEndpoint.Validate(payment: null);

        ReferenceEquals(problem, WolverineContinue.NoProblems).ShouldBeTrue();
    }

    [Fact]
    public void validate_lets_an_approved_card_through()
    {
        var problem = PlaceOrderEndpoint.Validate(new Payment(Guid.NewGuid(), "approved", null));

        ReferenceEquals(problem, WolverineContinue.NoProblems).ShouldBeTrue();
    }

    [Fact]
    public void validate_rejects_a_declined_card_with_402_carrying_the_reason()
    {
        var problem = PlaceOrderEndpoint.Validate(new Payment(Guid.NewGuid(), "declined", "insufficient_funds"));

        problem.Status.ShouldBe(402);
        problem.Detail.ShouldBe("Card declined: insufficient_funds.");
    }

    [Fact]
    public void validate_answers_503_when_the_vendor_never_answered()
    {
        var problem = PlaceOrderEndpoint.Validate(new Payment(Guid.NewGuid(), "unavailable", null));

        problem.Status.ShouldBe(503);
        problem.Detail.ShouldBe("The payment service did not answer.");
    }
}
