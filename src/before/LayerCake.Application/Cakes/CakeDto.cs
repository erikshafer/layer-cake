namespace LayerCake.Application.Cakes;

/// <summary>
/// The shape the Application layer hands to the presentation layer.
/// Entities never cross this boundary.
/// </summary>
public sealed record CakeDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public DateTimeOffset PublishedAt { get; init; }
}
