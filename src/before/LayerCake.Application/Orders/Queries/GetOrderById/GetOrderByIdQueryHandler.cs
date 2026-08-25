using AutoMapper;
using LayerCake.Application.Common.Exceptions;
using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Entities;
using MediatR;

namespace LayerCake.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        // The read returns the stored snapshot and totals untouched; nothing
        // is recomputed here, so later cake edits can never drift the order.
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.Id);

        return _mapper.Map<OrderDto>(order);
    }
}
