namespace LayerCake.CleanTemplate.Domain.Entities;

public class Cake : BaseAuditableEntity<Guid>
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public DateTimeOffset PublishedAt { get; set; }
}
