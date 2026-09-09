using LayerCake.Slices.Orders;
using Shouldly;
using Xunit;

namespace LayerCake.Slices.Tests;

/// <summary>
/// The handler returns what should happen instead of doing it, so the test
/// inspects a value. No DbContext, real or faked.
/// </summary>
public class NotifyBakerHandlerTests
{
    [Fact]
    public void handle_returns_a_baker_task_keyed_by_the_order_id()
    {
        var orderId = Guid.NewGuid();

        var effect = NotifyBakerHandler.Handle(new NotifyBaker(orderId, "2x Chocolate Stout"));

        effect.OrderId.ShouldBe(orderId);
        effect.Summary.ShouldBe("2x Chocolate Stout");
        effect.CreatedAt.ShouldNotBe(default);
    }
}
