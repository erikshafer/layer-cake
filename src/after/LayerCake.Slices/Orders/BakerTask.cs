using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayerCake.Slices.Orders;

/// <summary>
/// A to-do item for the bakers, created by the NotifyBaker handler. The
/// order's id IS the primary key, so a redelivered message upserts instead
/// of duplicating: exactly one task per order, by construction.
/// </summary>
public class BakerTask
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Summary { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

public class BakerTaskTable : IEntityTypeConfiguration<BakerTask>
{
    public void Configure(EntityTypeBuilder<BakerTask> builder)
    {
        builder.ToTable("baker_tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Summary).HasMaxLength(1000);
    }
}
