using AutoMapper;
using LayerCake.Application.Cakes;
using LayerCake.Domain.Entities;

namespace LayerCake.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for cake entity-to-DTO projections.
/// </summary>
public sealed class CakeMappingProfile : Profile
{
    public CakeMappingProfile()
    {
        CreateMap<Cake, CakeDto>();
    }
}
