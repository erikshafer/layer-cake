using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;
using Wolverine.Http;

namespace LayerCake.Slices.Cakes;

public static class BrowseCakesEndpoint
{
    // AsNoTracking and [NonTransactional]: this is a pure read, so no change
    // tracker and no transaction around it.
    [NonTransactional]
    [WolverineGet("/cakes")]
    public static Task<List<Cake>> Get(LayerCakeDbContext db, CancellationToken ct)
        => db.Set<Cake>().AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
}
