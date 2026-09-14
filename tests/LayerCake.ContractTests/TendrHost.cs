using JasperFx.CommandLine;
using Marten;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tendr;
using Tendr.Authorizations;
using Wolverine;
using Xunit;

namespace LayerCake.ContractTests;

/// <summary>
/// The card vendor on a real socket, one per twin collection. Alba's
/// TestServer has no socket, and the point of slice 005 is that the call to
/// Tendr crosses one, so Tendr runs on Kestrel at 127.0.0.1 on a port the OS
/// picks, against the collection's own PostgreSQL container (schema "tendr").
/// No new container.
/// </summary>
/// <remarks>
/// xUnit collection fixtures cannot take each other as constructor
/// arguments, so the first twin host fixture in the collection starts this
/// one with the database's connection string. Classes in a collection run
/// one at a time, so the lazy start never races. Tendr's data is never
/// reset: every order id, and so every idempotency key, is fresh.
/// </remarks>
public sealed class TendrHostFixture : IAsyncLifetime
{
    /// <summary>
    /// Nothing listens here, so a connection is refused at once: the
    /// outage fixtures point a twin at this to take Tendr "down".
    /// </summary>
    public const string ClosedPortUrl = "http://127.0.0.1:1";

    private TendrFactory? _factory;

    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// A plain HttpClient over the same socket the twins use, for asserting
    /// what Tendr recorded.
    /// </summary>
    public HttpClient Client { get; private set; } = null!;

    public async Task<string> StartAsync(string connectionString)
    {
        if (_factory is not null)
        {
            return BaseUrl;
        }

        // Tendr's Program.cs ends in RunJasperFxCommands, which only runs the
        // host it built when JasperFx is told a test factory owns it. Alba
        // sets this for the twins; this host is not built by Alba.
        JasperFxEnvironment.AutoStartHost = true;

        _factory = new TendrFactory(connectionString);
        _factory.UseKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0));

        await WolverineHostGate.BuildAsync(typeof(TendrApi).Assembly, () => _factory.StartServer());

        var addresses = _factory.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>();
        BaseUrl = addresses.Addresses.Single();
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        return BaseUrl;
    }

    /// <summary>
    /// Every authorization id Tendr holds. Tendr keys an authorization by the
    /// idempotency key, which both twins set to the order id, so a new id
    /// here is an order a twin tried to pay for.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> AuthorizationIdsAsync()
    {
        await using var session = _factory!.Services.GetRequiredService<IDocumentStore>().QuerySession();

        var authorizations = await session.Query<Authorization>().ToListAsync();

        return authorizations.Select(a => a.Id).ToList();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    private sealed class TendrFactory : WebApplicationFactory<TendrApi>
    {
        private readonly string _connectionString;

        public TendrFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Postgres", _connectionString);
            builder.ConfigureServices(services => services.RunWolverineInSoloMode());
        }
    }
}
