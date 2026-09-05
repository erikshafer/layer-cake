using Wolverine.Http;

namespace LayerCake.Slices.Features;

/// <summary>
/// Smoke endpoint from the scaffold, kept on purpose: the suite and the demo
/// page use it to confirm a twin is up before touching the real routes.
/// </summary>
public static class PingEndpoint
{
    [WolverineGet("/ping")]
    public static string Get() => "pong";
}
