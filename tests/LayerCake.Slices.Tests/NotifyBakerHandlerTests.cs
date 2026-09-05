using LayerCake.Slices.Orders;
using Shouldly;
using Wolverine.Persistence;
using Xunit;

namespace LayerCake.Slices.Tests;

/// <summary>
/// The handler returns what should be stored instead of storing it, so the
/// test inspects a value. No IDocumentSession, real or faked.
/// </summary>
public class NotifyBakerHandlerTests
{
    [Fact]
    public void handle_returns_an_upsert_keyed_by_the_order_id()
    {
        var orderId = Guid.NewGuid();

        var action = NotifyBakerHandler.Handle(new NotifyBaker(orderId, "2x Chocolate Stout"));

        var store = action.ShouldBeOfType<Store<BakerTask>>();
        store.Entity.Id.ShouldBe(orderId);
        store.Entity.OrderId.ShouldBe(orderId);
        store.Entity.Summary.ShouldBe("2x Chocolate Stout");
        store.Entity.CreatedAt.ShouldNotBe(default);
    }
}
