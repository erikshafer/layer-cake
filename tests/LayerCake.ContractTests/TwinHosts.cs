using Alba;
using LayerCake.Infrastructure.Persistence;
using LayerCake.Slices;
using LayerCake.WebApi;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LayerCake.ContractTests;

/// <summary>
/// Scenario classes for the same twin share an xUnit collection so they run
/// sequentially: each class fixture resets that twin's schema and re-seeds,
/// and parallel hosts would race on startup migrations and seed data.
/// The two collections still run in parallel with each other; the twins
/// never touch each other's schema.
/// </summary>
[CollectionDefinition(Name)]
public sealed class BeforeTwinCollection
{
    public const string Name = "before twin";
}

[CollectionDefinition(Name)]
public sealed class AfterTwinCollection
{
    public const string Name = "after twin";
}

/// <summary>
/// Boots the before twin (Clean Architecture, EF Core + MediatR) for a test
/// class, then resets the "before" schema and re-seeds so every scenario
/// class starts from the same three-cake catalog.
/// Requires the docker-compose PostgreSQL to be running.
/// </summary>
public sealed class BeforeHostFixture : IAsyncLifetime
{
    public IAlbaHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Host = await AlbaHost.For<BeforeTwin>();

        using var scope = Host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LayerCakeDbContext>();
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
/// class, then wipes the "after" schema's documents and re-seeds.
/// Requires the docker-compose PostgreSQL to be running.
/// </summary>
public sealed class AfterHostFixture : IAsyncLifetime
{
    public IAlbaHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Host = await AlbaHost.For<AfterTwin>();

        await Host.CleanAllMartenDataAsync();
        await SeedData.ApplyAsync(Host.Services.GetRequiredService<IDocumentStore>());
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
    }
}
