using Alba;
using LayerCake.Infrastructure.Persistence;
using LayerCake.Slices;
using LayerCake.WebApi;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Wolverine;
using Xunit;

namespace LayerCake.ContractTests;

/// <summary>
/// One throwaway PostgreSQL container per twin collection, started by
/// Testcontainers so "dotnet test" needs Docker running and nothing else:
/// no docker compose, no shared volume, no leftover state between runs.
/// Same image as docker-compose.yml, so the engine the suite proves against
/// is the engine the live demo runs on.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}

/// <summary>
/// One throwaway RabbitMQ container per twin collection, the same way as
/// PostgreSQL: since slice 004 the baker notification crosses a real broker
/// on both twins, so the suite starts one and "dotnet test" still needs
/// Docker and nothing else. Same major as docker-compose.yml.
/// </summary>
public sealed class RabbitMqContainerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4").Build();

    public string ConnectionString => _rabbitMq.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _rabbitMq.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _rabbitMq.DisposeAsync();
    }
}

/// <summary>
/// Scenario classes for the same twin share an xUnit collection so they run
/// sequentially: each class fixture resets that twin's schema and re-seeds,
/// and parallel hosts would race on startup migrations and seed data.
/// The two collections still run in parallel with each other, each on its
/// own database and broker containers, so the twins never touch each
/// other's data or consume each other's messages.
/// </summary>
[CollectionDefinition(Name)]
public sealed class BeforeTwinCollection : ICollectionFixture<PostgresContainerFixture>, ICollectionFixture<RabbitMqContainerFixture>
{
    public const string Name = "before twin";
}

[CollectionDefinition(Name)]
public sealed class AfterTwinCollection : ICollectionFixture<PostgresContainerFixture>, ICollectionFixture<RabbitMqContainerFixture>
{
    public const string Name = "after twin";
}

/// <summary>
/// Boots the before twin (Clean Architecture, EF Core + MediatR) for a test
/// class against the collection's containers, applies the EF Core migrations,
/// then resets the "before" schema and re-seeds so every scenario class
/// starts from the same three-cake catalog.
/// </summary>
public sealed class BeforeHostFixture : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private readonly RabbitMqContainerFixture _rabbitMq;

    public BeforeHostFixture(PostgresContainerFixture postgres, RabbitMqContainerFixture rabbitMq)
    {
        _postgres = postgres;
        _rabbitMq = rabbitMq;
    }

    public IAlbaHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Host = await AlbaHost.For<BeforeTwin>(x =>
        {
            x.UseSetting("ConnectionStrings:Postgres", _postgres.ConnectionString);
            x.UseSetting("ConnectionStrings:RabbitMq", _rabbitMq.ConnectionString);
        });

        using var scope = Host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LayerCakeDbContext>();

        // Program.cs only migrates in Development; the fixture owns it here so
        // the suite never depends on how Alba names the environment.
        await dbContext.Database.MigrateAsync();

        await dbContext.BakerTasks.ExecuteDeleteAsync();
        // Raw SQL, not ExecuteDelete: Order owns its lines in a separate
        // table, and the FK's ON DELETE CASCADE clears them with the orders.
        await dbContext.Database.ExecuteSqlRawAsync("""DELETE FROM "before"."Orders";""");
        await dbContext.Cakes.ExecuteDeleteAsync();
        await dbContext.Coupons.ExecuteDeleteAsync();
        await LayerCakeDbContextSeeder.SeedAsync(dbContext);
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
    }
}

/// <summary>
/// Boots the after twin (vertical slices, Wolverine + Marten) for a test
/// class against the collection's containers, builds the "after" schema,
/// then wipes its documents and re-seeds.
/// </summary>
public sealed class AfterHostFixture : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private readonly RabbitMqContainerFixture _rabbitMq;

    public AfterHostFixture(PostgresContainerFixture postgres, RabbitMqContainerFixture rabbitMq)
    {
        _postgres = postgres;
        _rabbitMq = rabbitMq;
    }

    public IAlbaHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Host = await AlbaHost.For<AfterTwin>(x =>
        {
            x.UseSetting("ConnectionStrings:Postgres", _postgres.ConnectionString);
            // Configuration keys are case-insensitive, so this also satisfies
            // the after twin's lowercase "rabbitmq" read.
            x.UseSetting("ConnectionStrings:RabbitMq", _rabbitMq.ConnectionString);

            x.ConfigureServices(services =>
            {
                // Solo mode: no node registration or leader election, so each
                // sequential test host boots fast and leaves no stale node rows.
                // External transports are deliberately NOT stubbed: since slice
                // 004 NotifyBaker crosses the collection's real broker, and a
                // null sender would make the exactly-one-task scenario vacuous.
                services.RunWolverineInSoloMode();
            });
        });

        var store = Host.Services.GetRequiredService<IDocumentStore>();

        // The Marten counterpart of MigrateAsync: build every configured
        // table up front instead of leaning on AutoCreate during the first
        // scenario, so the wipe below has something to wipe on a fresh box.
        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        await Host.CleanAllMartenDataAsync();
        await SeedData.ApplyAsync(store);
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
    }
}
