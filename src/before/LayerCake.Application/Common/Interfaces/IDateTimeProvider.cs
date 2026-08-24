namespace LayerCake.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the system clock so Application logic never reaches for
/// DateTimeOffset.UtcNow directly. Implemented in Infrastructure.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
