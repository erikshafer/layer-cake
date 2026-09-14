using Alba;
using Shouldly;
using Xunit;

namespace LayerCake.ContractTests;

/// <summary>
/// Slice 005 with the vendor down: each twin boots with Tendr:BaseUrl on a
/// closed port. A card order answers 503 and places nothing; an order with
/// no card never needed Tendr and goes through. No retry on either twin, so
/// the refused connection is the whole story.
/// </summary>
public abstract class PaymentOutageScenarios
{
    protected abstract IAlbaHost Host { get; }

    private OrderProbe Orders => new(Host);

    [Fact]
    public async Task place_order_with_card_while_tendr_is_down_returns_503_and_creates_no_order()
    {
        var stout = await Orders.FindCakeAsync("Chocolate Stout");
        var tasksBefore = (await Orders.BakerTasksAsync()).Select(t => t.OrderId).ToList();

        var result = await Orders.PostOrderAsync(new
        {
            lines = new[] { new { cakeId = stout.Id, quantity = 1 } },
            card = new { number = "4242 4242 4242 4242" }
        }, 503);

        await result.ShouldBeProblem(503, "The payment service did not answer.");

        await Orders.ShouldSeeNoNewBakerTaskAsync(tasksBefore);
    }

    [Fact]
    public async Task place_order_without_card_while_tendr_is_down_returns_201()
    {
        var yellow = await Orders.FindCakeAsync("Classic Yellow");

        var (placed, body) = await Orders.PlaceAsync(new
        {
            lines = new[] { new { cakeId = yellow.Id, quantity = 1 } }
        });

        body.ShouldNotContain("payment");

        // Waiting for the task also keeps it from landing inside the other
        // scenario's no-new-task window.
        var tasks = await Orders.WaitForBakerTasksAsync(placed.Id);
        tasks.Count.ShouldBe(1);
    }
}

[Collection(BeforeTwinCollection.Name)]
public sealed class BeforeTwinPaymentOutage : PaymentOutageScenarios, IClassFixture<BeforeOutageHostFixture>
{
    private readonly BeforeOutageHostFixture _fixture;

    public BeforeTwinPaymentOutage(BeforeOutageHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}

[Collection(AfterTwinCollection.Name)]
public sealed class AfterTwinPaymentOutage : PaymentOutageScenarios, IClassFixture<AfterOutageHostFixture>
{
    private readonly AfterOutageHostFixture _fixture;

    public AfterTwinPaymentOutage(AfterOutageHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}
