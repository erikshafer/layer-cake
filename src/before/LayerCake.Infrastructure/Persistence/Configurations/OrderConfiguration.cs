using LayerCake.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayerCake.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Order"/>. Lines are owned children in
/// their own table; money columns share the catalog's numeric(10,2)
/// precision. CakeId is a snapshot value, deliberately not a foreign key.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Subtotal)
            .HasPrecision(10, 2);

        builder.Property(o => o.Discount)
            .HasPrecision(10, 2);

        builder.Property(o => o.Total)
            .HasPrecision(10, 2);

        builder.Property(o => o.CouponCode)
            .HasMaxLength(50);

        builder.OwnsMany(o => o.Lines, lines =>
        {
            lines.ToTable("OrderLines");
            lines.WithOwner().HasForeignKey("OrderId");

            lines.Property(l => l.Name)
                .IsRequired()
                .HasMaxLength(200);

            lines.Property(l => l.UnitPrice)
                .HasPrecision(10, 2);

            lines.Property(l => l.LineTotal)
                .HasPrecision(10, 2);
        });
    }
}
