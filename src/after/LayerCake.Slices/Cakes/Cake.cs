using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayerCake.Slices.Cakes;

/// <summary>
/// A layer cake offered by the bakery. An EF Core entity, and the after
/// twin's entire persistence model for this feature.
/// </summary>
public class Cake
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTimeOffset PublishedAt { get; set; }
}

/// <summary>
/// The table sits with the type it maps, not in the DbContext.
/// ApplyConfigurationsFromAssembly picks it up.
/// </summary>
public class CakeTable : IEntityTypeConfiguration<Cake>
{
    public void Configure(EntityTypeBuilder<Cake> builder)
    {
        builder.ToTable("cakes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Price).HasPrecision(10, 2);

        // The before twin has the same index. The 409 guard in PublishCake is
        // backed by the database on both twins, not just a check-then-insert.
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
