using LayerCake.Domain.Entities;

namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// Abstraction over order persistence so the Application layer stays
/// ignorant of EF Core. Implemented in Infrastructure. Unlike the cake and
/// coupon repositories, Add does not save: the order must commit in the
/// same transaction as its baker task, so the handler saves once through
/// <see cref="IUnitOfWork"/>.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Order order);
}
