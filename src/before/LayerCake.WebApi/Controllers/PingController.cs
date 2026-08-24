using Microsoft.AspNetCore.Mvc;

namespace LayerCake.WebApi.Controllers;

/// <summary>
/// Scaffold smoke endpoint: proves the twin hosts answer the same contract
/// before the first real feature lands.
/// </summary>
[ApiController]
[Route("ping")]
public sealed class PingController : ControllerBase
{
    [HttpGet]
    public string Get() => "pong";
}
