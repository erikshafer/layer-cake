using CritterWatch.Services.Hosting;
using Wolverine.CritterWatch;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// User secrets only auto-load in Development; load explicitly so the license key
// is still found if this host is ever run as Production (the Development
// environment substitutes a "Development" license tier and never reads the key).
builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

// CritterWatch's own event store: a dedicated database, deliberately NOT a
// schema inside "layercake" (see docker/postgres-init/).
var postgresConnection = builder.Configuration.GetConnectionString("critterwatch")
    ?? "Host=localhost;Port=5432;Database=critterwatch;Username=postgres;Password=postgres";

builder.AddCritterWatch(postgresConnection, opts =>
{
    // Same broker the after twin publishes telemetry to.
    opts.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("rabbitmq") ?? "amqp://localhost"))
        .AutoProvision();

    // The well-known intake queue. NativeAck keeps Buffered throughput with
    // Inline's no-loss guarantee, and CritterWatch partitions the lanes by
    // service internally, so one service's telemetry still applies in order
    // against its event stream. Plain Inline or Sequential fails Wolverine
    // 6.30's listener-mode validation here.
    opts.ListenToRabbitQueue("critterwatch")
        .ProcessInParallelWithNativeAcks()
        .UseCritterWatchSerializer();
});
// Single-node is the default; clustering is opt-in (configureClusterShardedTopology).

var app = builder.Build();

// Wolverine HTTP endpoints + SignalR hub + the embedded SPA.
app.UseCritterWatch();

await app.RunAsync();
