using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using LayerCake.Slices.Cakes;
using LayerCake.Slices.Coupons;
using LayerCake.Slices.Payments;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;
using Wolverine.Http;

namespace LayerCake.Slices.Orders;

public record PlaceOrder(List<PlaceOrderLine>? Lines, string? CouponCode, Card? Card = null);

public record PlaceOrderLine(Guid CakeId, int Quantity);

/// <summary>
/// Everything the guards and the decide step need, loaded ONCE by LoadAsync
/// and handed to Validate and Post as a method parameter.
/// </summary>
public record PlaceOrderData(IReadOnlyList<Cake> Cakes, Coupon? Coupon);

/// <summary>
/// 201 response for a freshly placed order, mirroring the stored document.
/// IHttpAware writes the status code and Location header itself (same
/// pattern as PublishedCake in slice 001).
/// </summary>
public record PlacedOrder(
    Guid Id,
    List<OrderLine> Lines,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? CouponCode,
    DateTimeOffset PlacedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    Payment? Payment) : IHttpAware
{
    public static PlacedOrder From(Order order)
        => new(order.Id, order.Lines, order.Subtotal, order.Discount, order.Total, order.CouponCode, order.PlacedAt, order.Payment);

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        => builder.Metadata.Add(new ProducesResponseTypeMetadata(201, typeof(PlacedOrder), ["application/json"]));

    void IHttpAware.Apply(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status201Created;
        context.Response.Headers.Location = $"/orders/{Id}";
    }
}

public static class PlaceOrderEndpoint
{
    // Load once: the referenced cakes and (when a code was sent) the coupon.
    // Wolverine passes this method's return value into Validate and Post.
    public static async Task<PlaceOrderData> LoadAsync(
        PlaceOrder command,
        LayerCakeDbContext db,
        CancellationToken ct)
    {
        var cakeIds = (command.Lines ?? []).Select(l => l.CakeId).Distinct().ToArray();

        var cakes = cakeIds.Length == 0
            ? []
            : await db.Set<Cake>().AsNoTracking().Where(c => cakeIds.Contains(c.Id)).ToListAsync(ct);

        var couponCode = command.CouponCode?.ToUpperInvariant();

        var coupon = couponCode is null
            ? null
            : await db.Set<Coupon>().AsNoTracking().FirstOrDefaultAsync(c => c.Code == couponCode, ct);

        return new PlaceOrderData(cakes, coupon);
    }

    // The contract's ordered guard chain; first failure wins, and Post only
    // ever sees the happy path. Wolverine short-circuits with the
    // ProblemDetails as application/problem+json.
    public static ProblemDetails Validate(PlaceOrder command, PlaceOrderData data)
    {
        // Guard 1: lines non-empty, every quantity >= 1.
        if (command.Lines is null || command.Lines.Count == 0)
        {
            return new ProblemDetails { Detail = "Lines must not be empty", Status = 400 };
        }

        if (command.Lines.Any(l => l.Quantity < 1))
        {
            return new ProblemDetails { Detail = "Quantity must be at least 1", Status = 400 };
        }

        // Guard 2: every referenced cake must exist; list the offenders.
        var missingIds = command.Lines
            .Select(l => l.CakeId)
            .Distinct()
            .Except(data.Cakes.Select(c => c.Id))
            .ToList();

        if (missingIds.Count > 0)
        {
            return new ProblemDetails
            {
                Detail = $"Unknown cake ids: {string.Join(", ", missingIds)}.",
                Status = 422
            };
        }

        // Guard 3: a present coupon must evaluate to valid, via THE shared
        // function from slice 002. Checkout is authoritative; a bad coupon
        // fails the order, never silently drops.
        if (!string.IsNullOrWhiteSpace(command.CouponCode))
        {
            var status = CouponValidation.Evaluate(data.Coupon, DateTimeOffset.UtcNow);
            if (status != CouponStatus.Valid)
            {
                var wireStatus = JsonNamingPolicy.CamelCase.ConvertName(status.ToString());
                return new ProblemDetails
                {
                    Detail = $"Coupon \"{command.CouponCode.ToUpperInvariant()}\" is not valid: {wireStatus}.",
                    Status = 422
                };
            }
        }

        return WolverineContinue.NoProblems;
    }

    // The second rung on the load leg. The vendor needs the total, and the total
    // is the decision's, so Decide runs here; Post only stores what this returns.
    // No card, no call: the bakery takes payment at pickup. Wolverine finds
    // Load, Before and Validate by name; any other rung needs [WolverineBefore],
    // and rungs run in the order they are declared.
    [WolverineBefore]
    public static async Task<(Order, Payment?)> AuthorizeAsync(
        PlaceOrder command,
        PlaceOrderData data,
        TendrClient tendr,
        CancellationToken ct)
    {
        var order = Decide(command.Lines!, data.Cakes, data.Coupon, DateTimeOffset.UtcNow);

        if (command.Card is null)
        {
            return (order, null);
        }

        try
        {
            var authorization = await tendr.AuthorizeAsync(order.Id, order.Total, command.Card.Number, ct);

            return (order, new Payment(authorization.Id, authorization.Status, authorization.Reason));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            // Tendr refused the connection or ran past the client's two-second
            // timeout. An exception escaping here would be a bare 500, so the
            // outage becomes a value and the guard below answers for it.
            return (order, new Payment(order.Id, "unavailable", null));
        }
    }

    // Guard 4: a card that was sent must have been approved. 402, because it is
    // the customer's card that failed, not the request's shape; 503 when the
    // vendor never answered.
    public static ProblemDetails Validate(Payment? payment)
    {
        if (payment is { Status: "declined" })
        {
            return new ProblemDetails { Detail = $"Card declined: {payment.Reason}.", Status = 402 };
        }

        if (payment is { Status: "unavailable" })
        {
            return new ProblemDetails { Detail = "The payment service did not answer.", Status = 503 };
        }

        return WolverineContinue.NoProblems;
    }

    // The command stays first although Post never reads it: Wolverine.Http
    // takes the request body's type from the endpoint method's own
    // parameters, and every rung above needs the body.
    [WolverinePost("/orders")]
    public static (PlacedOrder, NotifyBaker) Post(PlaceOrder command, Order order, Payment? payment, LayerCakeDbContext db)
    {
        order.PaymentStatus = payment?.Status ?? "atPickup";
        order.PaymentAuthorizationId = payment?.AuthorizationId;

        db.Add(order);

        // The A-Frame beat: storing the order and sending NotifyBaker are one
        // atomic act. The cascaded message rides Wolverine's outbox in the SAME
        // transaction AutoApplyTransactions commits, so no order without a
        // baker task, no baker task without an order.
        var summary = string.Join(", ", order.Lines.Select(l => $"{l.Quantity}x {l.Name}"));

        return (PlacedOrder.From(order), new NotifyBaker(order.Id, summary));
    }

    // The whole pricing model, pure: lines + cakes + optional coupon in,
    // priced order out. Snapshots names and prices at placement time.
    public static Order Decide(
        List<PlaceOrderLine> lines,
        IReadOnlyList<Cake> cakes,
        Coupon? coupon,
        DateTimeOffset placedAt)
    {
        var cakesById = cakes.ToDictionary(c => c.Id);

        var orderLines = lines
            .Select(l =>
            {
                var cake = cakesById[l.CakeId];
                return new OrderLine
                {
                    CakeId = cake.Id,
                    Name = cake.Name,
                    UnitPrice = cake.Price,
                    Quantity = l.Quantity,
                    LineTotal = cake.Price * l.Quantity
                };
            })
            .ToList();

        var subtotal = orderLines.Sum(l => l.LineTotal);
        var discount = coupon is null
            ? 0m
            : Math.Round(subtotal * coupon.PercentOff / 100m, 2, MidpointRounding.ToEven);

        return new Order
        {
            Id = Guid.NewGuid(),
            Lines = orderLines,
            Subtotal = subtotal,
            Discount = discount,
            Total = subtotal - discount,
            CouponCode = coupon?.Code,
            PlacedAt = placedAt
        };
    }
}
