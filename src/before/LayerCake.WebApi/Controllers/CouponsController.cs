using LayerCake.Application.Coupons;
using LayerCake.Application.Coupons.Queries.ValidateCoupon;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LayerCake.WebApi.Controllers;

/// <summary>
/// Thin HTTP adapter for coupon validation. Always 200: validation is a
/// question, and "unknown code" is a normal answer to a question, not a
/// missing resource.
/// </summary>
[ApiController]
[Route("coupons")]
public sealed class CouponsController : ControllerBase
{
    private readonly ISender _sender;

    public CouponsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{code}")]
    [ProducesResponseType(typeof(CouponValidationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CouponValidationDto>> Validate(string code, CancellationToken cancellationToken)
    {
        return await _sender.Send(new ValidateCouponQuery(code), cancellationToken);
    }
}
