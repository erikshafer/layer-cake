using Microsoft.EntityFrameworkCore;

namespace LayerCake.Slices;

/// <summary>
/// The after twin's entire persistence model. Deliberately empty of features:
/// no DbSet properties and no per-entity mapping, because every table
/// configures itself in its own feature file and endpoints reach for
/// db.Set&lt;T&gt;(). Adding a slice adds files and edits nothing here.
/// </summary>
public class LayerCakeDbContext(DbContextOptions<LayerCakeDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Two schemas, one database: EF Core owns "before" for the layered
        // twin and "after" here, so the twins never touch each other's rows.
        modelBuilder.HasDefaultSchema("after");

        // The whole mapping story: every IEntityTypeConfiguration in this
        // assembly, wherever its feature folder happens to be.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LayerCakeDbContext).Assembly);
    }
}
