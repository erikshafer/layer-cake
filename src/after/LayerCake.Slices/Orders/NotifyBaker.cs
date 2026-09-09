using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.Attributes;

namespace LayerCake.Slices.Orders;

public record NotifyBaker(Guid OrderId, string Summary);

/// <summary>
/// What the handler decided should happen, as a value. Wolverine runs
/// ExecuteAsync after the handler returns and injects what it asks for, so
/// the DbContext never reaches the handler itself.
/// </summary>
public record AddBakerTask(Guid OrderId, string Summary, DateTimeOffset CreatedAt) : ISideEffect
{
    public async Task ExecuteAsync(LayerCakeDbContext db, CancellationToken ct)
    {
        // The broker delivers at least once. A document store would have
        // upserted on the identity for free; EF Core inserts, so redelivery
        // is something to check for. Exactly one task per order either way.
        if (await db.Set<BakerTask>().AnyAsync(t => t.Id == OrderId, ct))
        {
            return;
        }

        db.Add(new BakerTask
        {
            Id = OrderId,
            OrderId = OrderId,
            Summary = Summary,
            CreatedAt = CreatedAt
        });
    }
}

public static class NotifyBakerHandler
{
    // A pure function: no DbContext, no await. It returns what should happen
    // and the transactional middleware does it, so this can be unit tested
    // with nothing but the message. [Transactional] because the handler
    // itself takes no DbContext for AutoApplyTransactions to notice.
    [Transactional]
    public static AddBakerTask Handle(NotifyBaker message)
        => new(message.OrderId, message.Summary, DateTimeOffset.UtcNow);
}
