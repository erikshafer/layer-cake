using MediatR;

namespace LayerCake.Application.Cakes.Commands.PublishCake;

/// <summary>
/// Publishes a new cake to the catalog.
/// </summary>
public sealed record PublishCakeCommand(string? Name, string? Description, decimal Price) : IRequest<CakeDto>;
