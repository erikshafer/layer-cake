using LayerCake.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayerCake.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Cake"/>. The unique index on Name backs
/// the duplicate-name business rule checked in the Application layer.
/// </summary>
public sealed class CakeConfiguration : IEntityTypeConfiguration<Cake>
{
    public void Configure(EntityTypeBuilder<Cake> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.Price)
            .HasPrecision(10, 2);
    }
}
