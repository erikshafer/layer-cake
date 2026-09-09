using System.Text.Json.Serialization;
using Marten;
using Wolverine.Http;

namespace LayerCake.Slices.Coupons;

/// <summary>
/// The always-200 discriminated envelope. percentOff appears only when the
/// coupon is actually valid; null means absent on the wire.
/// </summary>
public record CouponValidated(
    string Code,
    CouponStatus Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? PercentOff);

public static class ValidateCouponEndpoint
{
    // Deliberately not [Entity]: the route value needs uppercasing before it
    // can be the document identity, and a miss here is the normal "invalid"
    // answer, never a 404.
    [WolverineGet("/coupons/{code}")]
    public static async Task<CouponValidated> Get(string code, IQuerySession session, CancellationToken ct)
    {
        var canonical = code.ToUpperInvariant();
        var coupon = await session.LoadAsync<Coupon>(canonical, ct);

        var status = CouponValidation.Evaluate(coupon, DateTimeOffset.UtcNow);

        return new CouponValidated(canonical, status, status == CouponStatus.Valid ? coupon!.PercentOff : null);
    }
}
