using LayerCake.Application.Common.Interfaces;

namespace LayerCake.Infrastructure.Services;

/// <summary>
/// The real clock. Everything else asks <see cref="IDateTimeProvider"/>.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
