using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayerCake.Slices.Coupons;

/// <summary>
/// A discount coupon. The uppercase code IS the primary key, so a
/// case-insensitive lookup is one ToUpperInvariant plus a straight read.
/// </summary>
public class Coupon
{
    public string Code { get; set; } = string.Empty;

    public int PercentOff { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}

public class CouponTable : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(c => c.Code);
        builder.Property(c => c.Code).HasMaxLength(50);
    }
}
