using LayerCake.Domain.Common;
using LayerCake.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LayerCake.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the before twin. Uses the "before" schema so
/// both twins can share one PostgreSQL database without touching each other.
/// </summary>
public sealed class LayerCakeDbContext : DbContext
{
    public LayerCakeDbContext(DbContextOptions<LayerCakeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cake> Cakes => Set<Cake>();

    public DbSet<Coupon> Coupons => Set<Coupon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("before");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LayerCakeDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit stamping kept inline rather than in a SaveChanges interceptor;
        // one entity type does not justify the extra moving part.
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity<Guid>>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = DateTimeOffset.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
