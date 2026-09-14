using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using JasperFx.CommandLine;
using Marten;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Tendr.Authorizations;
using Wolverine;
using Wolverine.Http;
using Wolverine.Http.Transport;
using Xunit;

namespace Tendr.Tests;

/// <summary>
/// The pocket answer to "and if the other side were mine?": a Wolverine app
/// sends AuthorizeCard as a message over Wolverine's HTTP transport and gets
/// the CardAuthorization back as the reply. No HttpClient in the test; the
/// transport is the client.
/// </summary>
[Collection(TendrCollection.Name)]
public sealed class HttpTransportFacts
{
    private readonly TendrFixture _fixture;

    public HttpTransportFacts(TendrFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task a_wolverine_sender_gets_an_approval_back_through_invoke_async()
    {
        // The transport posts to a URL, so this Tendr listens on a real socket.
        // HTTPS because the pinned Wolverine 6.30.0 only resolves the https
        // scheme for this transport (http:// arrives in 6.34.0), with a
        // throwaway certificate so the fact needs no dev cert on the machine.
        JasperFxEnvironment.AutoStartHost = true;

        using var certificate = CreateLoopbackCertificate();

        await using var tendr = new KestrelTendr(_fixture.ConnectionString);
        tendr.UseKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0, listen => listen.UseHttps(certificate)));
        tendr.StartServer();

        var tendrUrl = tendr.Services.GetRequiredService<IServer>().Features
            .GetRequiredFeature<IServerAddressesFeature>().Addresses.Single();

        using var sender = await Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                // This test process already hosts Tendr, and Wolverine would
                // otherwise adopt Tendr's assembly for this host's discovery.
                opts.ApplicationAssembly = typeof(HttpTransportFacts).Assembly;

                opts.PublishMessage<AuthorizeCard>().ToHttpEndpoint($"{tendrUrl}/_wolverine/invoke");
            })
            .ConfigureServices(services =>
            {
                // Registers the transport's client; the sender needs nothing
                // else from Wolverine.Http.
                services.AddWolverineHttp();

                // The transport's own named client, told to trust the
                // throwaway certificate. Configuration, not a call.
                services.AddHttpClient(HttpTransport.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
            })
            .StartAsync();

        var authorization = await sender.MessageBus()
            .InvokeAsync<CardAuthorization>(new AuthorizeCard(6120, "usd", new Card("4242 4242 4242 4242")));

        authorization.Status.ShouldBe("approved");
        authorization.Reason.ShouldBeNull();
        authorization.AmountCents.ShouldBe(6120);
        authorization.CardLast4.ShouldBe("4242");

        // It was Tendr that answered: the authorization is in Tendr's store,
        // keyed by the envelope id the message carried.
        await using var session = tendr.Services.GetRequiredService<IDocumentStore>().QuerySession();
        var stored = await session.LoadAsync<Authorization>(authorization.Id);
        stored.ShouldNotBeNull().Status.ShouldBe("approved");
    }

    private static X509Certificate2 CreateLoopbackCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=127.0.0.1", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var names = new SubjectAlternativeNameBuilder();
        names.AddIpAddress(System.Net.IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());

        using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));

        // Round-trip through PKCS#12 so the private key is usable by Kestrel on every OS.
        return X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pkcs12), null);
    }

    private sealed class KestrelTendr : WebApplicationFactory<TendrApi>
    {
        private readonly string _connectionString;

        public KestrelTendr(string connectionString)
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
