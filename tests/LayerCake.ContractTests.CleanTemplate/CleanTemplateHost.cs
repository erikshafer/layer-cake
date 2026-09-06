extern alias cleantemplate;

using Alba;
using LayerCake.CleanTemplate.Domain.Entities;
using LayerCake.CleanTemplate.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace LayerCake.ContractTests;

/// <summary>
/// Experiment (branch experiment/before-clean-template only): the same cake scenarios
/// run against slice 001 built on the Clean Architecture Solution Template (`dotnet new ca-sln`)
/// (experiments/before-clean-template). Its own collection and container, the same
/// way as the two twins; no coupon or order scenarios, because the template
/// host has nothing for them to run against.
/// </summary>
[CollectionDefinition(Name)]
public sealed class CleanTemplateCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "clean template";
}

/// <summary>
/// Boots the template's Web host for a test class against the collection's
/// container and runs the template's own initialiser, which drops, recreates
/// and seeds the database, so every scenario class starts from the same
/// three-cake catalog.
/// </summary>
public sealed class CleanTemplateHostFixture : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;

    public CleanTemplateHostFixture(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
    }

    public IAlbaHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // The template initialises with EnsureDeleted + EnsureCreated, not
        // migrations. It cannot drop the container's default "postgres"
        // database while connected to it, so the template gets its own
        // database name; EnsureCreated creates it on first use.
        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.ConnectionString)
        {
            Database = "layercake_clean_template"
        }.ConnectionString;

        Host = await AlbaHost.For<cleantemplate::Program>(x =>
        {
            // The key the template's Infrastructure reads (Services.Database).
            x.UseSetting("ConnectionStrings:LayerCake.CleanTemplateDb", connectionString);
        });

        // Program.cs only initialises in Development; the fixture owns it here
        // the same way the before fixture owns MigrateAsync. The template's
        // initialiser is also the reset: drop, recreate, seed.
        using var scope = Host.Services.CreateScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
    }
}

[Collection(CleanTemplateCollection.Name)]
public sealed class CleanTemplatePing : PingScenarios, IClassFixture<CleanTemplateHostFixture>
{
    private readonly CleanTemplateHostFixture _fixture;

    public CleanTemplatePing(CleanTemplateHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;
}

[Collection(CleanTemplateCollection.Name)]
public sealed class CleanTemplateCakes : CakeScenarios, IClassFixture<CleanTemplateHostFixture>
{
    private readonly CleanTemplateHostFixture _fixture;

    public CleanTemplateCakes(CleanTemplateHostFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAlbaHost Host => _fixture.Host;

    protected override async Task InsertCakeDirectlyAsync(string name)
    {
        // The DbContext directly, outside MediatR: the handler's AnyAsync
        // check never runs, so only the unique index stands in the way.
        using var scope = Host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Cakes.Add(new Cake
        {
            Name = name,
            Description = "Direct insert",
            Price = 1.00m,
            PublishedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
}
