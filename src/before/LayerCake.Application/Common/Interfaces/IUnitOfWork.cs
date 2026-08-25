namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// The transaction boundary. PlaceOrder writes an order AND a baker task
/// atomically, so those repositories only stage changes and the handler
/// commits them here in one save.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
