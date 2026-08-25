namespace LayerCake.Application.Common.Exceptions;

/// <summary>
/// Thrown when an order references cake ids that do not exist. Mapped to a
/// 422 in the WebApi layer; the message lists the offending ids.
/// </summary>
public sealed class UnknownCakesException : Exception
{
    public UnknownCakesException(IReadOnlyList<Guid> cakeIds)
        : base($"Unknown cake ids: {string.Join(", ", cakeIds)}.")
    {
        CakeIds = cakeIds;
    }

    public IReadOnlyList<Guid> CakeIds { get; }
}
