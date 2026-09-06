namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// The transaction boundary. Repositories only stage changes; the handler
/// that owns the use case commits them here in one save. PlaceOrder commits
/// the order and then publishes; CreateBakerTask commits the task.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
