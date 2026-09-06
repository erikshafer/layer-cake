using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Entities;
using LayerCake.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LayerCake.Infrastructure.Repositories;

/// <summary>
/// EF Core-backed implementation of the Application layer's baker-task
/// repository abstraction. Add stages only; the caller commits through
/// IUnitOfWork so the task shares the order's transaction.
/// </summary>
public sealed class BakerTaskRepository : IBakerTaskRepository
{
    private readonly LayerCakeDbContext _dbContext;

    public BakerTaskRepository(LayerCakeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<BakerTask>> GetAllAsync(Guid? orderId, CancellationToken cancellationToken)
    {
        var query = _dbContext.BakerTasks.AsNoTracking();

        if (orderId is not null)
        {
            query = query.Where(t => t.OrderId == orderId);
        }

        return await query
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _dbContext.BakerTasks
            .AsNoTracking()
            .AnyAsync(t => t.OrderId == orderId, cancellationToken);
    }

    public void Add(BakerTask bakerTask)
    {
        _dbContext.BakerTasks.Add(bakerTask);
    }
}
