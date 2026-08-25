using LayerCake.Slices.Orders;
using Marten;
using Wolverine.Http;

namespace LayerCake.Slices.Features;

/// <summary>
/// One item on the bakers' to-do list as the wire sees it (no document id;
/// the order id is the interesting one).
/// </summary>
public record BakerTaskItem(Guid OrderId, string Summary, DateTimeOffset CreatedAt);

public static class GetBakerTasksEndpoint
{
    // IQuerySession, not IDocumentSession: this is a pure read. orderId is
    // an optional query-string filter so the contract suite can probe for
    // one order's task.
    [WolverineGet("/baker/tasks")]
    public static async Task<IReadOnlyList<BakerTaskItem>> Get(
        Guid? orderId,
        IQuerySession session,
        CancellationToken ct)
    {
        IQueryable<BakerTask> query = session.Query<BakerTask>();

        if (orderId is not null)
        {
            query = query.Where(t => t.OrderId == orderId);
        }

        var tasks = await query.OrderBy(t => t.CreatedAt).ToListAsync(ct);

        return tasks.Select(t => new BakerTaskItem(t.OrderId, t.Summary, t.CreatedAt)).ToList();
    }
}
