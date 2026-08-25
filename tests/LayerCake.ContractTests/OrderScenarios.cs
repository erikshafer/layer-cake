using System.Text.Json;
using Alba;
using Shouldly;
using Xunit;

namespace LayerCake.ContractTests;

/// <summary>
/// Slice 003 contract: POST /orders (ordered guards, coupon-aware totals,
/// price snapshots), GET /orders/{id}, and the baker-task side effect via
/// GET /baker/tasks. Written once, run against both twins by the sealed
/// subclasses at the bottom. The baker-task scenario polls: it neither
/// knows nor cares which twin produces the task asynchronously.
/// </summary>
public abstract class OrderScenarios
{
    protected abstract IAlbaHost Host { get; }

    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(5);

    private sealed record CakeItem(Guid Id, string Name, decimal Price);

    private sealed record OrderLineItem(Guid CakeId, string Name, decimal UnitPrice, int Quantity, decimal LineTotal);

    private sealed record OrderEnvelope(
        Guid Id,
        List<OrderLineItem> Lines,
        decimal Subtotal,
        decimal Discount,
        decimal Total,
        string? CouponCode,
        DateTimeOffset PlacedAt);

    private sealed record BakerTaskItem(Guid OrderId, string Summary, DateTimeOffset CreatedAt);

    private async Task<CakeItem> FindCakeAsync(string name)
    {
        var result = await Host.Scenario(s =>
        {
            s.Get.Url("/cakes");
            s.StatusCodeShouldBe(200);
        });

        var body = await result.ReadAsTextAsync();
        var cakes = JsonSerializer.Deserialize<List<CakeItem>>(body, Json)!;

        return cakes.Single(c => c.Name == name);
    }

    private async Task<(OrderEnvelope Envelope, string RawBody, string Location)> PlaceAsync(object payload)
    {
        var result = await Host.Scenario(s =>
        {
            s.Post.Json(payload).ToUrl("/orders");
            s.StatusCodeShouldBe(201);
        });

        var body = await result.ReadAsTextAsync();
        var location = result.Context.Response.Headers.Location.ToString();

        return (JsonSerializer.Deserialize<OrderEnvelope>(body, Json)!, body, location);
    }

    /// <summary>
    /// Polls the baker to-do list for one order until a task appears or the
    /// timeout passes; the failure message carries the last body seen.
    /// </summary>
    private async Task<List<BakerTaskItem>> WaitForBakerTasksAsync(Guid orderId)
    {
        var deadline = DateTimeOffset.UtcNow + PollTimeout;
        var lastBody = "(never polled)";

        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = await Host.Scenario(s =>
            {
                s.Get.Url($"/baker/tasks?orderId={orderId}");
                s.StatusCodeShouldBe(200);
            });

            lastBody = await result.ReadAsTextAsync();
            var tasks = JsonSerializer.Deserialize<List<BakerTaskItem>>(lastBody, Json)!;

            if (tasks.Count > 0)
            {
                return tasks;
            }

            await Task.Delay(PollInterval);
        }

        throw new ShouldAssertException(
            $"No baker task appeared for order {orderId} within {PollTimeout.TotalSeconds}s; last body: {lastBody}");
    }

    [Fact]
    public async Task place_order_returns_201_with_totals()
    {
        var stout = await FindCakeAsync("Chocolate Stout");
        var lemon = await FindCakeAsync("Lemon Chiffon");

        var (order, _, location) = await PlaceAsync(new
        {
            lines = new[]
            {
                new { cakeId = stout.Id, quantity = 2 },
                new { cakeId = lemon.Id, quantity = 1 }
            }
        });

        order.Id.ShouldNotBe(Guid.Empty);
        location.ShouldEndWith($"/orders/{order.Id}");

        order.Lines.Count.ShouldBe(2);

        var stoutLine = order.Lines.Single(l => l.CakeId == stout.Id);
        stoutLine.Name.ShouldBe("Chocolate Stout");
        stoutLine.UnitPrice.ShouldBe(34.00m);
        stoutLine.Quantity.ShouldBe(2);
        stoutLine.LineTotal.ShouldBe(68.00m);

        var lemonLine = order.Lines.Single(l => l.CakeId == lemon.Id);
        lemonLine.UnitPrice.ShouldBe(28.00m);
        lemonLine.LineTotal.ShouldBe(28.00m);

        order.Subtotal.ShouldBe(96.00m);
        order.Discount.ShouldBe(0m);
        order.Total.ShouldBe(96.00m);
        order.PlacedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task place_order_without_coupon_has_zero_discount()
    {
        var yellow = await FindCakeAsync("Classic Yellow");

        var (order, body, _) = await PlaceAsync(new
        {
            lines = new[] { new { cakeId = yellow.Id, quantity = 1 } }
        });

        order.Subtotal.ShouldBe(24.00m);
        order.Discount.ShouldBe(0m);
        order.Total.ShouldBe(24.00m);

        // Absent means the key never appears, not "couponCode": null.
        body.ShouldNotContain("couponCode");
    }

    [Fact]
    public async Task place_order_with_empty_lines_returns_400()
    {
        var result = await Host.Scenario(s =>
        {
            s.Post.Json(new { lines = Array.Empty<object>() }).ToUrl("/orders");
            s.StatusCodeShouldBe(400);
        });

        await result.ShouldBeProblem(400, "lines");
    }

    [Fact]
    public async Task place_order_with_zero_quantity_returns_400()
    {
        var yellow = await FindCakeAsync("Classic Yellow");

        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { lines = new[] { new { cakeId = yellow.Id, quantity = 0 } } })
                .ToUrl("/orders");
            s.StatusCodeShouldBe(400);
        });

        await result.ShouldBeProblem(400, "quantity");
    }

    [Fact]
    public async Task place_order_with_unknown_cake_returns_422_listing_ids()
    {
        var unknownId = Guid.NewGuid();

        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { lines = new[] { new { cakeId = unknownId, quantity = 1 } } })
                .ToUrl("/orders");
            s.StatusCodeShouldBe(422);
        });

        // The problem detail must list the offending id(s).
        await result.ShouldBeProblem(422, unknownId.ToString());
    }

    [Fact]
    public async Task place_order_with_valid_coupon_applies_discount_math()
    {
        var stout = await FindCakeAsync("Chocolate Stout");

        var (order, _, _) = await PlaceAsync(new
        {
            lines = new[] { new { cakeId = stout.Id, quantity = 2 } },
            couponCode = "BDAY10"
        });

        // subtotal 68.00, 10% off: discount 6.80, total 61.20 (ToEven at 2dp).
        order.Subtotal.ShouldBe(68.00m);
        order.Discount.ShouldBe(6.80m);
        order.Total.ShouldBe(61.20m);
        order.CouponCode.ShouldBe("BDAY10");
    }

    [Fact]
    public async Task place_order_with_expired_coupon_returns_422_with_status()
    {
        var yellow = await FindCakeAsync("Classic Yellow");

        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new
                {
                    lines = new[] { new { cakeId = yellow.Id, quantity = 1 } },
                    couponCode = "SUMMER25"
                })
                .ToUrl("/orders");
            s.StatusCodeShouldBe(422);
        });

        await result.ShouldBeProblem(422, "expired");
    }

    [Fact]
    public async Task place_order_with_not_yet_active_coupon_returns_422_with_status()
    {
        var yellow = await FindCakeAsync("Classic Yellow");

        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new
                {
                    lines = new[] { new { cakeId = yellow.Id, quantity = 1 } },
                    couponCode = "HOLIDAY30"
                })
                .ToUrl("/orders");
            s.StatusCodeShouldBe(422);
        });

        await result.ShouldBeProblem(422, "notYetActive");
    }

    [Fact]
    public async Task get_order_by_id_returns_200_with_same_shape()
    {
        var stout = await FindCakeAsync("Chocolate Stout");

        var (placed, _, _) = await PlaceAsync(new
        {
            lines = new[] { new { cakeId = stout.Id, quantity = 2 } },
            couponCode = "BDAY10"
        });

        var result = await Host.Scenario(s =>
        {
            s.Get.Url($"/orders/{placed.Id}");
            s.StatusCodeShouldBe(200);
        });

        var body = await result.ReadAsTextAsync();
        var fetched = JsonSerializer.Deserialize<OrderEnvelope>(body, Json)!;

        // The read is the stored placement snapshot, value for value.
        fetched.Id.ShouldBe(placed.Id);
        fetched.Lines.Count.ShouldBe(placed.Lines.Count);
        fetched.Lines.Single().CakeId.ShouldBe(stout.Id);
        fetched.Lines.Single().Name.ShouldBe("Chocolate Stout");
        fetched.Lines.Single().UnitPrice.ShouldBe(34.00m);
        fetched.Lines.Single().Quantity.ShouldBe(2);
        fetched.Lines.Single().LineTotal.ShouldBe(68.00m);
        fetched.Subtotal.ShouldBe(placed.Subtotal);
        fetched.Discount.ShouldBe(placed.Discount);
        fetched.Total.ShouldBe(placed.Total);
        fetched.CouponCode.ShouldBe("BDAY10");
        // Tolerance: PostgreSQL timestamptz stores microseconds, so the
        // before twin's read-back drops the sub-microsecond ticks the POST
        // response carried. Accepted storage-precision divergence.
        fetched.PlacedAt.ShouldBe(placed.PlacedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task get_missing_order_returns_404()
    {
        var result = await Host.Scenario(s =>
        {
            s.Get.Url($"/orders/{Guid.NewGuid()}");
            s.StatusCodeShouldBe(404);
        });

        await result.ShouldBeProblem(404, "not found");
    }

    [Fact]
    public async Task placing_order_produces_exactly_one_baker_task()
    {
        var stout = await FindCakeAsync("Chocolate Stout");

        var (order, _, _) = await PlaceAsync(new
        {
            lines = new[] { new { cakeId = stout.Id, quantity = 2 } }
        });

        // Poll until present, then assert exactly one: the contract does not
        // reveal which twin writes the task asynchronously.
        var tasks = await WaitForBakerTasksAsync(order.Id);

        tasks.Count.ShouldBe(1);
        tasks[0].OrderId.ShouldBe(order.Id);
        tasks[0].Summary.ShouldNotBeNullOrWhiteSpace();
        tasks[0].CreatedAt.ShouldNotBe(default);
    }
}

[Collection(BeforeTwinCollection.Name)]
public sealed class BeforeTwinOrders : OrderScenarios, IClassFixture<BeforeHostFixture>
{
    private readonly BeforeHostFixture _fixture;

    public BeforeTwinOrders(BeforeHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}

[Collection(AfterTwinCollection.Name)]
public sealed class AfterTwinOrders : OrderScenarios, IClassFixture<AfterHostFixture>
{
    private readonly AfterHostFixture _fixture;

    public AfterTwinOrders(AfterHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}
