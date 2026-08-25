using MediatR;

namespace LayerCake.Application.Orders.Commands.PlaceOrder;

/// <summary>
/// Places an order for one or more cakes, optionally with a coupon code.
/// </summary>
public sealed record PlaceOrderCommand(List<PlaceOrderLineRequest>? Lines, string? CouponCode) : IRequest<OrderDto>;

/// <summary>
/// One requested line: which cake, and how many.
/// </summary>
public sealed record PlaceOrderLineRequest(Guid CakeId, int Quantity);
