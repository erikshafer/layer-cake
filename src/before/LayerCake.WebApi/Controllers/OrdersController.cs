using LayerCake.Application.Orders;
using LayerCake.Application.Orders.Commands.PlaceOrder;
using LayerCake.Application.Orders.Queries.GetOrderById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LayerCake.WebApi.Controllers;

/// <summary>
/// Thin HTTP adapter: binds requests, sends them through MediatR, maps
/// results to ActionResults. All behavior lives further down the stack.
/// </summary>
[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> Place(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetOrderByIdQuery(id), cancellationToken);
    }
}
