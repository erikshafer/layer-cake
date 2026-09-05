namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the system clock for logic whose outcome depends on "now"
/// (coupon window evaluation). Plain timestamps such as PublishedAt and PlacedAt
/// still use DateTimeOffset.UtcNow directly. Implemented in Infrastructure.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
