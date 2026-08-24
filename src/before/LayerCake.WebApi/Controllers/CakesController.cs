using LayerCake.Application.Cakes;
using LayerCake.Application.Cakes.Commands.PublishCake;
using LayerCake.Application.Cakes.Queries.BrowseCakes;
using LayerCake.Application.Cakes.Queries.GetCakeById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LayerCake.WebApi.Controllers;

/// <summary>
/// Thin HTTP adapter: binds requests, sends them through MediatR, maps
/// results to ActionResults. All behavior lives further down the stack.
/// </summary>
[ApiController]
[Route("cakes")]
public sealed class CakesController : ControllerBase
{
    private readonly ISender _sender;

    public CakesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CakeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CakeDto>> Publish(PublishCakeCommand command, CancellationToken cancellationToken)
    {
        var cake = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = cake.Id }, cake);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CakeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CakeDto>>> Browse(CancellationToken cancellationToken)
    {
        return await _sender.Send(new BrowseCakesQuery(), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CakeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CakeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetCakeByIdQuery(id), cancellationToken);
    }
}
