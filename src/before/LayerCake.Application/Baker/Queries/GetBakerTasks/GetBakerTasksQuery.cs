using MediatR;

namespace LayerCake.Application.Baker.Queries.GetBakerTasks;

/// <summary>
/// Reads the bakers' to-do list, optionally filtered to one order.
/// </summary>
public sealed record GetBakerTasksQuery(Guid? OrderId) : IRequest<List<BakerTaskDto>>;
