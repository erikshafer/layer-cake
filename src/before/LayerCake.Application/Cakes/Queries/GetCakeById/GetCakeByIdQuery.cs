using MediatR;

namespace LayerCake.Application.Cakes.Queries.GetCakeById;

/// <summary>
/// Fetches a single cake; exists because POST's Location header points here.
/// </summary>
public sealed record GetCakeByIdQuery(Guid Id) : IRequest<CakeDto>;
