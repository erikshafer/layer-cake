using Wolverine.Http;

namespace Tendr;

/// <summary>
/// Smoke endpoint, like the twins' ping: something to wait on before the
/// first authorization.
/// </summary>
public static class PingEndpoint
{
    [WolverineGet("/ping")]
    public static string Get() => "pong";
}
