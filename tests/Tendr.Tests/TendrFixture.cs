using Alba;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wolverine;
using Xunit;

namespace Tendr.Tests;

/// <summary>
/// One throwaway PostgreSQL and one Tendr host for the whole project. Tendr's
/// data is never reset: every scenario uses a fresh idempotency key, so no
/// scenario can see another's authorization.
/// </summary>
public sealed class TendrFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public IAlbaHost Host { get; private set; } = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Host = await AlbaHost.For<TendrApi>(x =>
        {
            x.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());

            // Solo mode: no node registration or leader election on a test host.
            x.ConfigureServices(services => services.RunWolverineInSoloMode());
        });
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class TendrCollection : ICollectionFixture<TendrFixture>
{
    public const string Name = "tendr";
}
