using System.Text.Json.Serialization;

namespace LayerCake.Application.Coupons;

/// <summary>
/// The always-200 discriminated envelope. PercentOff stays null unless the
/// coupon is valid, and null means absent on the wire, never "percentOff": null.
/// </summary>
public sealed record CouponValidationDto
{
    public string Code { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PercentOff { get; init; }
}
