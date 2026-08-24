using System.Text.Json;
using Alba;
using Shouldly;
using Xunit;

namespace LayerCake.ContractTests;

/// <summary>
/// Slice 001 contract: POST /cakes, GET /cakes, GET /cakes/{id}. Written
/// once, run against both twins by the sealed subclasses at the bottom.
/// Each class run starts from the freshly seeded three-cake catalog
/// (see TwinHosts.cs), so "Chocolate Stout" already exists and publishing
/// it again is the natural duplicate.
/// </summary>
public abstract class CakeScenarios
{
    protected abstract IAlbaHost Host { get; }

    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    private sealed record CakeItem(Guid Id, string Name, string Description, decimal Price, DateTimeOffset PublishedAt);

    private async Task<List<CakeItem>> BrowseAsync()
    {
        var result = await Host.Scenario(s =>
        {
            s.Get.Url("/cakes");
            s.StatusCodeShouldBe(200);
        });

        var body = await result.ReadAsTextAsync();
        return JsonSerializer.Deserialize<List<CakeItem>>(body, Json)!;
    }

    [Fact]
    public async Task publish_cake_returns_201_with_location_and_body()
    {
        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { name = "Opera", description = "Coffee, chocolate, almond", price = 36.00m })
                .ToUrl("/cakes");
            s.StatusCodeShouldBe(201);
        });

        var body = await result.ReadAsTextAsync();
        var cake = JsonSerializer.Deserialize<CakeItem>(body, Json)!;

        cake.Id.ShouldNotBe(Guid.Empty);
        cake.Name.ShouldBe("Opera");
        cake.Description.ShouldBe("Coffee, chocolate, almond");
        cake.Price.ShouldBe(36.00m);
        cake.PublishedAt.ShouldNotBe(default);

        result.Context.Response.Headers.Location.ToString().ShouldEndWith($"/cakes/{cake.Id}");
    }

    [Fact]
    public async Task publish_cake_with_missing_name_returns_400()
    {
        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { description = "No name at all", price = 10.00m })
                .ToUrl("/cakes");
            s.StatusCodeShouldBe(400);
        });

        await result.ShouldBeProblem(400, "name");
    }

    [Fact]
    public async Task publish_cake_with_nonpositive_price_returns_400()
    {
        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { name = "Free Cake", description = "Suspiciously free", price = 0.00m })
                .ToUrl("/cakes");
            s.StatusCodeShouldBe(400);
        });

        await result.ShouldBeProblem(400, "price");
    }

    [Fact]
    public async Task publish_cake_with_duplicate_name_returns_409()
    {
        var result = await Host.Scenario(s =>
        {
            s.Post
                .Json(new { name = "Chocolate Stout", description = "Six layers, no mercy", price = 34.00m })
                .ToUrl("/cakes");
            s.StatusCodeShouldBe(409);
        });

        await result.ShouldBeProblem(409, "Chocolate Stout");
    }

    [Fact]
    public async Task browse_returns_seeded_cakes()
    {
        var cakes = await BrowseAsync();

        cakes.ShouldContain(c => c.Name == "Classic Yellow" && c.Description == "Three layers, vanilla buttercream" && c.Price == 24.00m);
        cakes.ShouldContain(c => c.Name == "Chocolate Stout" && c.Description == "Six layers, no mercy" && c.Price == 34.00m);
        cakes.ShouldContain(c => c.Name == "Lemon Chiffon" && c.Description == "Light, tart, dangerously easy" && c.Price == 28.00m);
    }

    [Fact]
    public async Task browse_includes_newly_published_cake()
    {
        await Host.Scenario(s =>
        {
            s.Post
                .Json(new { name = "Carrot Cake", description = "The vegetable defense", price = 26.00m })
                .ToUrl("/cakes");
            s.StatusCodeShouldBe(201);
        });

        var cakes = await BrowseAsync();

        cakes.ShouldContain(c => c.Name == "Carrot Cake" && c.Description == "The vegetable defense" && c.Price == 26.00m);
    }

    [Fact]
    public async Task get_cake_by_id_returns_200()
    {
        // Ids differ per twin, so discover one through browse.
        var target = (await BrowseAsync()).First();

        var result = await Host.Scenario(s =>
        {
            s.Get.Url($"/cakes/{target.Id}");
            s.StatusCodeShouldBe(200);
        });

        var body = await result.ReadAsTextAsync();
        var cake = JsonSerializer.Deserialize<CakeItem>(body, Json)!;

        cake.Id.ShouldBe(target.Id);
        cake.Name.ShouldBe(target.Name);
        cake.Description.ShouldBe(target.Description);
        cake.Price.ShouldBe(target.Price);
    }

    [Fact]
    public async Task get_missing_cake_returns_404()
    {
        var result = await Host.Scenario(s =>
        {
            s.Get.Url($"/cakes/{Guid.NewGuid()}");
            s.StatusCodeShouldBe(404);
        });

        await result.ShouldBeProblem(404, "not found");
    }
}

[Collection(BeforeTwinCollection.Name)]
public sealed class BeforeTwinCakes : CakeScenarios, IClassFixture<BeforeHostFixture>
{
    private readonly BeforeHostFixture _fixture;

    public BeforeTwinCakes(BeforeHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}

[Collection(AfterTwinCollection.Name)]
public sealed class AfterTwinCakes : CakeScenarios, IClassFixture<AfterHostFixture>
{
    private readonly AfterHostFixture _fixture;

    public AfterTwinCakes(AfterHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}
