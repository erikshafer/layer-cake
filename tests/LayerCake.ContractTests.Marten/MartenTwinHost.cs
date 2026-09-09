extern alias marten;

using Alba;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Xunit;
using MartenSeedData = marten::LayerCake.Slices.SeedData;
using MartenTwin = marten::LayerCake.Slices.AfterTwin;

namespace LayerCake.ContractTests;

/// <summary>
/// Experiment (2026-09-09): the same scenarios run against the after twin as
/// it stood on Marten (experiments/after-marten/), a third host with its own
/// collection and its own containers. The core twins moved to EF Core so both
/// sides of the talk share one ORM; this keeps the document-store version
/// honest and runnable.
/// </summary>
[CollectionDefinition(Name)]
public sealed class MartenTwinCollection : ICollectionFixture<PostgresContainerFixture>, ICollectionFixture<RabbitMqContainerFixture>
{
    public const string Name = "marten twin";
}

/// <summary>
/// Boots the Marten twin for a test class against the collection's
/// containers, builds the "after" schema, then wipes its documents and
/// re-seeds. Unchanged from the fixture the solution used before the move.
/// </summary>
public sealed class MartenHostFixture : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private readonly RabbitMqContainerFixture _rabbitMq;

    public MartenHostFixture(PostgresContainerFixture postgres, RabbitMqContainerFixture rabbitMq)
    {
        _postgres = postgres;
        _rabbitMq = rabbitMq;
    }

    public IAlbaHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Host = await AlbaHost.For<MartenTwin>(x =>
        {
            x.UseSetting("ConnectionStrings:Postgres", _postgres.ConnectionString);
            x.UseSetting("ConnectionStrings:RabbitMq", _rabbitMq.ConnectionString);

            x.ConfigureServices(services => services.RunWolverineInSoloMode());
        });

        var store = Host.Services.GetRequiredService<IDocumentStore>();

        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
        await Host.CleanAllMartenDataAsync();
        await MartenSeedData.ApplyAsync(store);
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
    }
}

[Collection(MartenTwinCollection.Name)]
public sealed class MartenTwinPing : PingScenarios, IClassFixture<MartenHostFixture>
{
    private readonly MartenHostFixture _fixture;

    public MartenTwinPing(MartenHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}

[Collection(MartenTwinCollection.Name)]
public sealed class MartenTwinCakes : CakeScenarios, IClassFixture<MartenHostFixture>
{
    private readonly MartenHostFixture _fixture;

    public MartenTwinCakes(MartenHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;

    protected override async Task InsertCakeDirectlyAsync(string name)
    {
        // A plain session outside any Wolverine handler, so ValidateAsync
        // never runs and only the unique index stands in the way.
        var store = Host.Services.GetRequiredService<IDocumentStore>();
        await using var session = store.LightweightSession();

        session.Store(new marten::LayerCake.Slices.Cakes.Cake
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Direct insert",
            Price = 1.00m,
            PublishedAt = DateTimeOffset.UtcNow
        });

        await session.SaveChangesAsync();
    }
}

[Collection(MartenTwinCollection.Name)]
public sealed class MartenTwinCoupons : CouponScenarios, IClassFixture<MartenHostFixture>
{
    private readonly MartenHostFixture _fixture;

    public MartenTwinCoupons(MartenHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}

[Collection(MartenTwinCollection.Name)]
public sealed class MartenTwinOrders : OrderScenarios, IClassFixture<MartenHostFixture>
{
    private readonly MartenHostFixture _fixture;

    public MartenTwinOrders(MartenHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}
