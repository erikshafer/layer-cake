using LayerCake.Domain.Entities;

namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// Abstraction over cake persistence so the Application layer stays
/// ignorant of EF Core. Implemented in Infrastructure.
/// </summary>
public interface ICakeRepository
{
    Task<Cake?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Cake>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Cake>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(Cake cake, CancellationToken cancellationToken);
}
