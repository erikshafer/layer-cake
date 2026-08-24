using LayerCake.Domain.Entities;

namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// Abstraction over coupon persistence so the Application layer stays
/// ignorant of EF Core. Implemented in Infrastructure. Codes are stored
/// canonically uppercase, so callers pass the uppercased form.
/// </summary>
public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken);
}
