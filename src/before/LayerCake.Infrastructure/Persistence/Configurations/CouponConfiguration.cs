using LayerCake.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayerCake.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Coupon"/>. Codes are stored canonically
/// uppercase; the unique index makes the code the coupon's business identity.
/// </summary>
public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.Code)
            .IsUnique();
    }
}
