using LayerCake.Slices.Cakes;
using Marten;
using Wolverine.Http;

namespace LayerCake.Slices.Features;

public static class BrowseCakesEndpoint
{
    // IQuerySession, not IDocumentSession: this is a pure read.
    [WolverineGet("/cakes")]
    public static Task<IReadOnlyList<Cake>> Get(IQuerySession session, CancellationToken ct)
        => session.Query<Cake>().OrderBy(c => c.Name).ToListAsync(ct);
}
