using LayerCake.Domain.Entities;

namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// Abstraction over baker-task persistence. Add stages only (no save); the
/// handler that stages a task commits it through <see cref="IUnitOfWork"/>.
/// </summary>
public interface IBakerTaskRepository
{
    Task<IReadOnlyList<BakerTask>> GetAllAsync(Guid? orderId, CancellationToken cancellationToken);

    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken);

    void Add(BakerTask bakerTask);
}
