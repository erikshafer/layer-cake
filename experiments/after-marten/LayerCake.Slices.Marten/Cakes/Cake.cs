namespace LayerCake.Slices.Cakes;

/// <summary>
/// A layer cake offered by the bakery. A Marten document, and the after
/// twin's entire persistence model for this feature.
/// </summary>
public class Cake
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTimeOffset PublishedAt { get; set; }
}
