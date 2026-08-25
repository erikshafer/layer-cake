namespace LayerCake.Application.Baker;

/// <summary>
/// One item on the bakers' to-do list: the observable proof that placing an
/// order notified the baker.
/// </summary>
public sealed record BakerTaskDto
{
    public Guid OrderId { get; init; }

    public string Summary { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}
