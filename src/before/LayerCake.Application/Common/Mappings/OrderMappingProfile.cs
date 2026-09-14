using AutoMapper;
using LayerCake.Application.Baker;
using LayerCake.Application.Orders;
using LayerCake.Domain.Entities;
using LayerCake.Domain.Enums;

namespace LayerCake.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for order and baker-task entity-to-DTO projections.
/// </summary>
public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        // The entity keeps payment as two flat columns; the DTO nests them.
        // Only an approved card reaches an order, and an order paid at pickup
        // has no payment at all.
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Payment, o => o.MapFrom(s => s.PaymentStatus == PaymentStatus.Approved
                ? new PaymentDto { AuthorizationId = s.PaymentAuthorizationId!.Value, Status = "approved" }
                : null));
        CreateMap<OrderLine, OrderLineDto>();
        CreateMap<BakerTask, BakerTaskDto>();
    }
}
