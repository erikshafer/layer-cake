using LayerCake.CleanTemplate.Domain.Entities;

namespace LayerCake.CleanTemplate.Application.Cakes;

public class CakeDto
{
    public Guid Id { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public DateTimeOffset PublishedAt { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Cake, CakeDto>();
        }
    }
}
