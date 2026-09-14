using System.Net.Http.Json;
using System.Text.Json;
using Alba;
using Shouldly;
using Xunit;

namespace LayerCake.ContractTests;

/// <summary>
/// The order surface as the payment scenarios see it: placing, reading back,
/// and probing the baker's to-do list. Shared by the payment and outage
/// scenario classes so neither has to reach into OrderScenarios.
/// </summary>
public sealed class OrderProbe
{
    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(5);

    // How long "no baker task appears" is watched for. A placed order's task
    // lands in well under this on both twins; waiting the full presence
    // timeout for every absence would add seconds to the live run.
    private static readonly TimeSpan AbsenceWindow = TimeSpan.FromSeconds(1);

    private readonly IAlbaHost _host;

    public OrderProbe(IAlbaHost host)
    {
        _host = host;
    }

    public sealed record CakeItem(Guid Id, string Name, decimal Price);

    public sealed record PaymentItem(Guid AuthorizationId, string Status);

    public sealed record PaidOrder(Guid Id, decimal Total, PaymentItem? Payment);

    public sealed record BakerTaskItem(Guid OrderId, string Summary, DateTimeOffset CreatedAt);

    public async Task<CakeItem> FindCakeAsync(string name)
    {
        var result = await _host.Scenario(s =>
        {
            s.Get.Url("/cakes");
            s.StatusCodeShouldBe(200);
        });

        var cakes = JsonSerializer.Deserialize<List<CakeItem>>(await result.ReadAsTextAsync(), Json)!;

        return cakes.Single(c => c.Name == name);
    }

    public async Task<IScenarioResult> PostOrderAsync(object payload, int expectedStatus)
    {
        return await _host.Scenario(s =>
        {
            s.Post.Json(payload).ToUrl("/orders");
            s.StatusCodeShouldBe(expectedStatus);
        });
    }

    public async Task<(PaidOrder Order, string RawBody)> PlaceAsync(object payload)
    {
        var result = await PostOrderAsync(payload, 201);
        var body = await result.ReadAsTextAsync();

        return (JsonSerializer.Deserialize<PaidOrder>(body, Json)!, body);
    }

    public async Task<(PaidOrder Order, string RawBody)> GetOrderAsync(Guid id)
    {
        var result = await _host.Scenario(s =>
        {
            s.Get.Url($"/orders/{id}");
            s.StatusCodeShouldBe(200);
        });

        var body = await result.ReadAsTextAsync();

        return (JsonSerializer.Deserialize<PaidOrder>(body, Json)!, body);
    }

    public async Task<List<BakerTaskItem>> BakerTasksAsync(Guid? orderId = null)
    {
        var result = await _host.Scenario(s =>
        {
            s.Get.Url(orderId is null ? "/baker/tasks" : $"/baker/tasks?orderId={orderId}");
            s.StatusCodeShouldBe(200);
        });

        return JsonSerializer.Deserialize<List<BakerTaskItem>>(await result.ReadAsTextAsync(), Json)!;
    }

    public async Task<List<BakerTaskItem>> WaitForBakerTasksAsync(Guid orderId)
    {
        var deadline = DateTimeOffset.UtcNow + PollTimeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            var tasks = await BakerTasksAsync(orderId);
            if (tasks.Count > 0)
            {
                return tasks;
            }

            await Task.Delay(PollInterval);
        }

        throw new ShouldAssertException($"No baker task appeared for order {orderId} within {PollTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Watches the whole to-do list for the absence window and fails if any
    /// task appears that was not there when watching began.
    /// </summary>
    public async Task ShouldSeeNoNewBakerTaskAsync(IReadOnlyCollection<Guid> orderIdsBefore)
    {
        var deadline = DateTimeOffset.UtcNow + AbsenceWindow;

        do
        {
            var newTasks = (await BakerTasksAsync()).Where(t => !orderIdsBefore.Contains(t.OrderId)).ToList();
            newTasks.ShouldBeEmpty();

            await Task.Delay(PollInterval);
        }
        while (DateTimeOffset.UtcNow < deadline);
    }

    /// <summary>
    /// The payment member exactly as the contract states it: two fields,
    /// camelCase, nothing else (no reason, no card digits).
    /// </summary>
    public static void ShouldHaveExactPaymentShape(string rawBody)
    {
        using var document = JsonDocument.Parse(rawBody);

        var payment = document.RootElement.GetProperty("payment");
        payment.EnumerateObject().Select(p => p.Name).ShouldBe(["authorizationId", "status"]);
    }
}

/// <summary>
/// Slice 005 contract: an optional card on POST /orders, authorized with
/// Tendr on a real socket after the three existing guards and before the
/// order is stored. Written once, run against both twins by the sealed
/// subclasses at the bottom. Kept apart from OrderScenarios so the Marten
/// experiment, which binds OrderScenarios and has no card path, is untouched.
/// </summary>
public abstract class PaymentScenarios
{
    private const string ApprovedCard = "4242 4242 4242 4242";
    private const string DecliningCard = "4000 0000 0000 0002";
    private const string InsufficientFundsCard = "4000 0000 0000 9995";

    protected abstract IAlbaHost Host { get; }

    protected abstract TendrHostFixture Tendr { get; }

    private OrderProbe Orders => new(Host);

    private sealed record TendrAuthorization(Guid Id, string Status, string? Reason, int AmountCents, string Currency, string CardLast4);

    [Fact]
    public async Task place_order_with_approved_card_returns_201_with_payment()
    {
        var stout = await Orders.FindCakeAsync("Chocolate Stout");

        var (placed, body) = await Orders.PlaceAsync(new
        {
            lines = new[] { new { cakeId = stout.Id, quantity = 2 } },
            couponCode = "BDAY10",
            card = new { number = ApprovedCard }
        });

        placed.Payment.ShouldNotBeNull();
        placed.Payment.Status.ShouldBe("approved");
        placed.Payment.AuthorizationId.ShouldBe(placed.Id);
        OrderProbe.ShouldHaveExactPaymentShape(body);

        // Tendr authorized exactly the discounted total, in cents, and kept
        // only the last four digits.
        var authorization = await Tendr.Client.GetFromJsonAsync<TendrAuthorization>($"/v1/authorizations/{placed.Id}");
        authorization.ShouldNotBeNull();
        authorization.Status.ShouldBe("approved");
        authorization.AmountCents.ShouldBe(6120);
        authorization.CardLast4.ShouldBe("4242");

        var (fetched, fetchedBody) = await Orders.GetOrderAsync(placed.Id);
        fetched.Payment.ShouldBe(placed.Payment);
        OrderProbe.ShouldHaveExactPaymentShape(fetchedBody);
    }

    [Fact]
    public async Task placing_order_with_approved_card_produces_exactly_one_baker_task()
    {
        var lemon = await Orders.FindCakeAsync("Lemon Chiffon");

        var (placed, _) = await Orders.PlaceAsync(new
        {
            lines = new[] { new { cakeId = lemon.Id, quantity = 1 } },
            card = new { number = ApprovedCard }
        });

        var tasks = await Orders.WaitForBakerTasksAsync(placed.Id);

        tasks.Count.ShouldBe(1);
        tasks[0].OrderId.ShouldBe(placed.Id);
    }

    [Fact]
    public async Task place_order_without_card_has_no_payment()
    {
        var yellow = await Orders.FindCakeAsync("Classic Yellow");
        var authorizationsBefore = await Tendr.AuthorizationIdsAsync();

        var (placed, body) = await Orders.PlaceAsync(new
        {
            lines = new[] { new { cakeId = yellow.Id, quantity = 1 } }
        });

        // Absent means the key never appears, not "payment": null.
        body.ShouldNotContain("payment");

        var (_, fetchedBody) = await Orders.GetOrderAsync(placed.Id);
        fetchedBody.ShouldNotContain("payment");

        // No card, no call.
        (await Tendr.AuthorizationIdsAsync()).ShouldBe(authorizationsBefore, ignoreOrder: true);
    }

    [Fact]
    public async Task place_order_with_declined_card_returns_402_and_creates_no_order()
    {
        var stout = await Orders.FindCakeAsync("Chocolate Stout");
        var authorizationsBefore = await Tendr.AuthorizationIdsAsync();
        var tasksBefore = (await Orders.BakerTasksAsync()).Select(t => t.OrderId).ToList();

        var result = await Orders.PostOrderAsync(new
        {
            lines = new[] { new { cakeId = stout.Id, quantity = 1 } },
            card = new { number = DecliningCard }
        }, 402);

        await result.ShouldBeProblem(402, "Card declined: card_declined.");

        // Tendr keyed the decline by the idempotency key, which is the order
        // id the twin generated: the one new authorization is the order that
        // never was.
        var wouldBeOrderId = (await Tendr.AuthorizationIdsAsync()).Except(authorizationsBefore).ShouldHaveSingleItem();

        var authorization = await Tendr.Client.GetFromJsonAsync<TendrAuthorization>($"/v1/authorizations/{wouldBeOrderId}");
        authorization.ShouldNotBeNull();
        authorization.Status.ShouldBe("declined");

        await Host.Scenario(s =>
        {
            s.Get.Url($"/orders/{wouldBeOrderId}");
            s.StatusCodeShouldBe(404);
        });

        await Orders.ShouldSeeNoNewBakerTaskAsync(tasksBefore);
    }

    [Fact]
    public async Task place_order_with_insufficient_funds_returns_402_with_reason()
    {
        var lemon = await Orders.FindCakeAsync("Lemon Chiffon");

        var result = await Orders.PostOrderAsync(new
        {
            lines = new[] { new { cakeId = lemon.Id, quantity = 2 } },
            card = new { number = InsufficientFundsCard }
        }, 402);

        await result.ShouldBeProblem(402, "Card declined: insufficient_funds.");
    }

    [Fact]
    public async Task place_order_with_bad_coupon_and_declining_card_returns_422_without_calling_tendr()
    {
        var yellow = await Orders.FindCakeAsync("Classic Yellow");
        var authorizationsBefore = await Tendr.AuthorizationIdsAsync();

        var result = await Orders.PostOrderAsync(new
        {
            lines = new[] { new { cakeId = yellow.Id, quantity = 1 } },
            couponCode = "SUMMER25",
            card = new { number = DecliningCard }
        }, 422);

        await result.ShouldBeProblem(422, "expired");

        // The ordering is the claim: the coupon guard failed first, so the
        // card never left the twin.
        (await Tendr.AuthorizationIdsAsync()).ShouldBe(authorizationsBefore, ignoreOrder: true);
    }
}

[Collection(BeforeTwinCollection.Name)]
public sealed class BeforeTwinPayments : PaymentScenarios, IClassFixture<BeforeHostFixture>
{
    private readonly BeforeHostFixture _fixture;

    public BeforeTwinPayments(BeforeHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;

    protected override TendrHostFixture Tendr => _fixture.Tendr;
}

[Collection(AfterTwinCollection.Name)]
public sealed class AfterTwinPayments : PaymentScenarios, IClassFixture<AfterHostFixture>
{
    private readonly AfterHostFixture _fixture;

    public AfterTwinPayments(AfterHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;

    protected override TendrHostFixture Tendr => _fixture.Tendr;
}
