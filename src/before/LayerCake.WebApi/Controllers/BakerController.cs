using LayerCake.Application.Baker;
using LayerCake.Application.Baker.Queries.GetBakerTasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LayerCake.WebApi.Controllers;

/// <summary>
/// The bakers' to-do surface: the synchronously observable proof that
/// placing an order notified the baker.
/// </summary>
[ApiController]
[Route("baker")]
public sealed class BakerController : ControllerBase
{
    private readonly ISender _sender;

    public BakerController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("tasks")]
    [ProducesResponseType(typeof(List<BakerTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BakerTaskDto>>> GetTasks(
        [FromQuery] Guid? orderId,
        CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetBakerTasksQuery(orderId), cancellationToken);
    }
}
