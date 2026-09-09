using LayerCake.Slices.Cakes;
using LayerCake.Slices.Coupons;
using Marten;

namespace LayerCake.Slices;

/// <summary>
/// Seeds the shared catalog (three cakes) and coupon book (three coupons)
/// both twins load identically. Idempotent so hosts and tests can call it
/// repeatedly.
/// </summary>
public static class SeedData
{
    public static async Task ApplyAsync(IDocumentStore store, CancellationToken ct = default)
    {
        // A plain session, not a handler: seeding owns its own commit.
        await using var session = store.LightweightSession();

        var existing = await session.Query<Cake>().Select(c => c.Name).ToListAsync(ct);

        Cake[] cakeSeeds =
        [
            new() { Name = "Classic Yellow", Description = "Three layers, vanilla buttercream", Price = 24.00m },
            new() { Name = "Chocolate Stout", Description = "Six layers, no mercy", Price = 34.00m },
            new() { Name = "Lemon Chiffon", Description = "Light, tart, dangerously easy", Price = 28.00m }
        ];

        foreach (var seed in cakeSeeds.Where(s => !existing.Contains(s.Name)))
        {
            seed.Id = Guid.NewGuid();
            seed.PublishedAt = DateTimeOffset.UtcNow;
            session.Store(seed);
        }

        // Coupon windows are relative to now (±1 year, ±1 day) so the seeded
        // statuses (valid, expired, notYetActive) never rot with the calendar.
        // The code is the document identity, so Store is an idempotent upsert:
        // seeding twice still leaves exactly three coupons.
        var now = DateTimeOffset.UtcNow;

        session.Store(
            new Coupon { Code = "BDAY10", PercentOff = 10, StartsAt = now.AddYears(-1), ExpiresAt = now.AddYears(1) },
            new Coupon { Code = "SUMMER25", PercentOff = 25, StartsAt = now.AddYears(-1), ExpiresAt = now.AddDays(-1) },
            new Coupon { Code = "HOLIDAY30", PercentOff = 30, StartsAt = now.AddDays(1), ExpiresAt = now.AddYears(1) });

        await session.SaveChangesAsync(ct);
    }
}
