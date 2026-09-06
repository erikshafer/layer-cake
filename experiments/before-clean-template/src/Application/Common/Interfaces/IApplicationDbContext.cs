using LayerCake.CleanTemplate.Domain.Entities;

namespace LayerCake.CleanTemplate.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    DbSet<Cake> Cakes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
