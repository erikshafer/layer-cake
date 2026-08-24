using Wolverine.Http;

namespace LayerCake.Slices.Features;

/// <summary>
/// Scaffold smoke endpoint: proves the twin hosts answer the same contract
/// before the first real slice lands.
/// </summary>
public static class PingEndpoint
{
    [WolverineGet("/ping")]
    public static string Get() => "pong";
}
