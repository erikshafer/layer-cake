using LayerCake.Slices.Orders;
using Wolverine.Persistence;

namespace LayerCake.Slices.Features;

public record NotifyBaker(Guid OrderId, string Summary);

public static class NotifyBakerHandler
{
    // A pure function: no session, no await. Storage.Store is a declarative
    // upsert that Wolverine's transactional middleware executes after Handle
    // returns, so this can be unit tested with nothing but the message. The
    // order's id is the document identity, so the outbox's at-least-once
    // redelivery can never create a second task for the same order.
    public static Store<BakerTask> Handle(NotifyBaker message)
        => Storage.Store(new BakerTask
        {
            Id = message.OrderId,
            OrderId = message.OrderId,
            Summary = message.Summary,
            CreatedAt = DateTimeOffset.UtcNow
        });
}
