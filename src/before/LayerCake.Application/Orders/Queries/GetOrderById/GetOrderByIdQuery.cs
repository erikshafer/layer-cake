using MediatR;

namespace LayerCake.Application.Orders.Queries.GetOrderById;

/// <summary>
/// Reads a placed order back by id.
/// </summary>
public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto>;
