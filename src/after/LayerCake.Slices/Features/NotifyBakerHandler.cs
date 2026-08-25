using LayerCake.Slices.Orders;
using Marten;

namespace LayerCake.Slices.Features;

public record NotifyBaker(Guid OrderId, string Summary);

public static class NotifyBakerHandler
{
    public static void Handle(NotifyBaker message, IDocumentSession session)
    {
        // The order's id is the document identity, so Store is an upsert:
        // the outbox's at-least-once redelivery can never create a second
        // task for the same order.
        session.Store(new BakerTask
        {
            Id = message.OrderId,
            OrderId = message.OrderId,
            Summary = message.Summary,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
