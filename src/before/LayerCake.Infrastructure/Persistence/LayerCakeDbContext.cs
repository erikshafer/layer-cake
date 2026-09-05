using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Common;
using LayerCake.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LayerCake.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the before twin. Uses the "before" schema so
/// both twins can share one PostgreSQL database without touching each other.
/// Doubles as the IUnitOfWork implementation: SaveChangesAsync IS the
/// transaction boundary handlers commit through.
/// </summary>
public sealed class LayerCakeDbContext : DbContext, IUnitOfWork
{
    public LayerCakeDbContext(DbContextOptions<LayerCakeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cake> Cakes => Set<Cake>();

    public DbSet<Coupon> Coupons => Set<Coupon>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<BakerTask> BakerTasks => Set<BakerTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("before");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LayerCakeDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit stamping kept inline rather than in a SaveChanges interceptor;
        // four small entity types do not justify the extra moving part.
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
