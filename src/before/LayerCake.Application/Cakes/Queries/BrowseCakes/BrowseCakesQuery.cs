using MediatR;

namespace LayerCake.Application.Cakes.Queries.BrowseCakes;

/// <summary>
/// Lists every cake in the catalog. A bakery has a dozen cakes; no paging.
/// </summary>
public sealed record BrowseCakesQuery : IRequest<List<CakeDto>>;
