using AutoMapper;
using LayerCake.Application.Baker;
using LayerCake.Application.Orders;
using LayerCake.Domain.Entities;

namespace LayerCake.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for order and baker-task entity-to-DTO projections.
/// </summary>
public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<OrderLine, OrderLineDto>();
        CreateMap<BakerTask, BakerTaskDto>();
    }
}
