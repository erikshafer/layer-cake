using LayerCake.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayerCake.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="BakerTask"/>. The index on OrderId backs
/// the to-do read's per-order filter.
/// </summary>
public sealed class BakerTaskConfiguration : IEntityTypeConfiguration<BakerTask>
{
    public void Configure(EntityTypeBuilder<BakerTask> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasIndex(t => t.OrderId);

        builder.Property(t => t.Summary)
            .IsRequired()
            .HasMaxLength(2000);
    }
}
