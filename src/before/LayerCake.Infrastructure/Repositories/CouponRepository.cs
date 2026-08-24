using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Entities;
using LayerCake.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LayerCake.Infrastructure.Repositories;

/// <summary>
/// EF Core-backed implementation of the Application layer's coupon
/// repository abstraction.
/// </summary>
public sealed class CouponRepository : ICouponRepository
{
    private readonly LayerCakeDbContext _dbContext;

    public CouponRepository(LayerCakeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }
}
