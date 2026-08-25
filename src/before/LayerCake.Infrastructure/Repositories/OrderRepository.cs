using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Entities;
using LayerCake.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LayerCake.Infrastructure.Repositories;

/// <summary>
/// EF Core-backed implementation of the Application layer's order
/// repository abstraction. Add stages only; the caller commits through
/// IUnitOfWork so the order and its baker task share one transaction.
/// </summary>
public sealed class OrderRepository : IOrderRepository
{
    private readonly LayerCakeDbContext _dbContext;

    public OrderRepository(LayerCakeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // Owned lines load with the order; no Include ceremony needed.
        return await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public void Add(Order order)
    {
        _dbContext.Orders.Add(order);
    }
}
