namespace LayerCake.Infrastructure.Payments;

/// <summary>
/// Settings for the Tendr card vendor, read from the Tendr section.
/// </summary>
public sealed class TendrOptions
{
    public const string SectionName = "Tendr";

    public string BaseUrl { get; set; } = "http://localhost:42040";

    public int TimeoutSeconds { get; set; } = 2;
}
