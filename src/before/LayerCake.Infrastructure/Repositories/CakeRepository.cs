using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Entities;
using LayerCake.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LayerCake.Infrastructure.Repositories;

/// <summary>
/// EF Core-backed implementation of the Application layer's cake
/// repository abstraction.
/// </summary>
public sealed class CakeRepository : ICakeRepository
{
    private readonly LayerCakeDbContext _dbContext;

    public CakeRepository(LayerCakeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Cake?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Cakes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Cake>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Cakes
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken)
    {
        return await _dbContext.Cakes.AnyAsync(c => c.Name == name, cancellationToken);
    }

    public async Task AddAsync(Cake cake, CancellationToken cancellationToken)
    {
        _dbContext.Cakes.Add(cake);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
