using LayerCake.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LayerCake.Infrastructure.Persistence;

/// <summary>
/// Seeds the shared catalog (three cakes) and coupon book (three coupons)
/// both twins load identically. Idempotent by name/code so hosts and tests
/// can call it repeatedly.
/// </summary>
public static class LayerCakeDbContextSeeder
{
    public static async Task SeedAsync(LayerCakeDbContext dbContext, CancellationToken cancellationToken = default)
    {
        Cake[] cakeSeeds =
        [
            new() { Name = "Classic Yellow", Description = "Three layers, vanilla buttercream", Price = 24.00m },
            new() { Name = "Chocolate Stout", Description = "Six layers, no mercy", Price = 34.00m },
            new() { Name = "Lemon Chiffon", Description = "Light, tart, dangerously easy", Price = 28.00m }
        ];

        foreach (var seed in cakeSeeds)
        {
            if (!await dbContext.Cakes.AnyAsync(c => c.Name == seed.Name, cancellationToken))
            {
                seed.Id = Guid.NewGuid();
                seed.PublishedAt = DateTimeOffset.UtcNow;
                dbContext.Cakes.Add(seed);
            }
        }

        // Coupon windows are relative to now (±1 year, ±1 day) so the seeded
        // statuses (valid, expired, notYetActive) never rot with the calendar.
        // Existing coupons get their windows refreshed on every startup; otherwise
        // a database seeded days earlier would drift (HOLIDAY30 would quietly turn
        // valid) and disagree with the after twin, which upserts on every run.
        var now = DateTimeOffset.UtcNow;

        Coupon[] couponSeeds =
        [
            new() { Code = "BDAY10", PercentOff = 10, StartsAt = now.AddYears(-1), ExpiresAt = now.AddYears(1) },
            new() { Code = "SUMMER25", PercentOff = 25, StartsAt = now.AddYears(-1), ExpiresAt = now.AddDays(-1) },
            new() { Code = "HOLIDAY30", PercentOff = 30, StartsAt = now.AddDays(1), ExpiresAt = now.AddYears(1) }
        ];

        foreach (var seed in couponSeeds)
        {
            var existing = await dbContext.Coupons
                .SingleOrDefaultAsync(c => c.Code == seed.Code, cancellationToken);

            if (existing is null)
            {
                seed.Id = Guid.NewGuid();
                dbContext.Coupons.Add(seed);
            }
            else
            {
                existing.PercentOff = seed.PercentOff;
                existing.StartsAt = seed.StartsAt;
                existing.ExpiresAt = seed.ExpiresAt;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
