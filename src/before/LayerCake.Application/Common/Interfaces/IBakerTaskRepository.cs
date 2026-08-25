using LayerCake.Domain.Entities;

namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// Abstraction over baker-task persistence. Add stages only (no save) so a
/// task always commits in the same transaction as the order that caused it;
/// the handler saves once through <see cref="IUnitOfWork"/>.
/// </summary>
public interface IBakerTaskRepository
{
    Task<IReadOnlyList<BakerTask>> GetAllAsync(Guid? orderId, CancellationToken cancellationToken);

    void Add(BakerTask bakerTask);
}
