using LayerCake.Slices.Cakes;
using LayerCake.Slices.Coupons;
using Microsoft.EntityFrameworkCore;

namespace LayerCake.Slices;

/// <summary>
/// Seeds the shared catalog (three cakes) and coupon book (three coupons)
/// both twins load identically. Idempotent so hosts and tests can call it
/// repeatedly.
/// </summary>
public static class SeedData
{
    public static async Task ApplyAsync(LayerCakeDbContext db, CancellationToken ct = default)
    {
        var existing = await db.Set<Cake>().Select(c => c.Name).ToListAsync(ct);

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
            db.Add(seed);
        }

        // Coupon windows are relative to now (±1 year, ±1 day) so the seeded
        // statuses (valid, expired, notYetActive) never rot with the calendar.
        // The code is the primary key, so seeding twice still leaves exactly
        // three coupons.
        var now = DateTimeOffset.UtcNow;
        var codes = await db.Set<Coupon>().Select(c => c.Code).ToListAsync(ct);

        Coupon[] couponSeeds =
        [
            new() { Code = "BDAY10", PercentOff = 10, StartsAt = now.AddYears(-1), ExpiresAt = now.AddYears(1) },
            new() { Code = "SUMMER25", PercentOff = 25, StartsAt = now.AddYears(-1), ExpiresAt = now.AddDays(-1) },
            new() { Code = "HOLIDAY30", PercentOff = 30, StartsAt = now.AddDays(1), ExpiresAt = now.AddYears(1) }
        ];

        db.AddRange(couponSeeds.Where(s => !codes.Contains(s.Code)));

        // Seeding owns its own commit; this is not a handler.
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Seeds once the host is up, which is after Wolverine has built the schema.
/// Registered in Development only; the contract suite seeds itself.
/// </summary>
public sealed class SeedOnStartup(IServiceProvider services) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        await SeedData.ApplyAsync(scope.ServiceProvider.GetRequiredService<LayerCakeDbContext>(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
