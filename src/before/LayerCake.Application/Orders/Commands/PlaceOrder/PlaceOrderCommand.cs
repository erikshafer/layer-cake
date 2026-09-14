using LayerCake.Application.Payments;
using MediatR;

namespace LayerCake.Application.Orders.Commands.PlaceOrder;

/// <summary>
/// Places an order for one or more cakes, optionally with a coupon code and
/// optionally paid by card. Without a card the order is paid at pickup.
/// </summary>
public sealed record PlaceOrderCommand(List<PlaceOrderLineRequest>? Lines, string? CouponCode, CardRequest? Card = null) : IRequest<OrderDto>;

/// <summary>
/// One requested line: which cake, and how many.
/// </summary>
public sealed record PlaceOrderLineRequest(Guid CakeId, int Quantity);
