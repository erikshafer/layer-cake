using JasperFx;
using JasperFx.Resources;
using LayerCake.Slices;
using LayerCake.Slices.Orders;
using LayerCake.Slices.Payments;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.CritterWatch;
using Wolverine.EntityFrameworkCore;
using Wolverine.Http;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=layercake;Username=postgres;Password=postgres";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddWolverineHttp();
builder.Services.AddCors();

// The card vendor. Base address from configuration; the contract suite points
// this at its own Tendr host through the same setting. A two-second timeout
// is the whole resilience story on both twins; see docs/slices/005.
builder.Services.AddHttpClient<TendrClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Tendr:BaseUrl"] ?? "http://localhost:42040");
    client.Timeout = TimeSpan.FromSeconds(2);
});

// Weasel builds the tables this DbContext describes, and Wolverine's own
// envelope tables, when the host starts. No migrations folder in this twin.
builder.Services.AddResourceSetupOnStartup();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(AfterTwin).Assembly);
    opts.ServiceName = "LayerCake";

    // IHttpClientFactory builds the typed client through a factory lambda that
    // Wolverine's codegen cannot see into, and Wolverine 6 refuses to resolve
    // such a service from the container unless the type is named here.
    opts.CodeGeneration.AlwaysUseServiceLocationFor<TendrClient>();

    // The same EF Core the before twin uses, registered Wolverine's way: the
    // DbContext options become a singleton, the transactional middleware
    // commits for the handlers, and the durable outbox writes its envelopes
    // through this same DbContext. The "after" schema keeps the twins apart
    // in one database.
    builder.Services.AddDbContextWithWolverineIntegration<LayerCakeDbContext>(
        o => o.UseNpgsql(connectionString));

    opts.PersistMessagesWithPostgresql(connectionString, "after");
    opts.UseEntityFrameworkCoreTransactions();
    opts.UseEntityFrameworkCoreWolverineManagedMigrations();
    opts.Policies.AutoApplyTransactions();

    // The broker is part of the app now, not just the telemetry channel.
    // AutoProvision declares the queue on startup; the contract suite points
    // this at its own Testcontainers broker through the same setting.
    opts.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("rabbitmq") ?? "amqp://localhost"))
        .AutoProvision();

    // The cascaded NotifyBaker goes to RabbitMQ. UseDurableOutbox keeps it an
    // outbox message: the envelope is written to the Wolverine table in the
    // SAME EF Core transaction as the order and sent to the broker after the
    // commit, replayed after a crash, delivered at least once. Without this
    // line "no order without a baker task" only holds while the process stays up.
    opts.PublishMessage<NotifyBaker>()
        .ToRabbitQueue("layercake-after-baker-tasks")
        .UseDurableOutbox();

    // The same host consumes the queue, so NotifyBakerHandler runs in-process
    // after the message has crossed the broker.
    opts.ListenToRabbitQueue("layercake-after-baker-tasks");

    // CritterWatch monitoring is opt-in via launchSettings (the live-demo run).
    // The contract tests boot this host through Alba without the flag, so
    // `dotnet test` never needs the console running.
    if (builder.Configuration.GetValue<bool>("CritterWatch:Enabled"))
    {
        // Telemetry out to the console's well-known intake queue; control
        // commands back on this service's private queue.
        opts.AddCritterWatchMonitoring(
            new Uri("rabbitmq://queue/critterwatch"),
            new Uri("rabbitmq://queue/layercake-control"));
    }
});

// Registered after Wolverine so it starts after the schema exists.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<SeedOnStartup>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Development-only CORS for the static demo page in src/frontend/ (opened
    // from disk, so its origin is "null"; AllowAnyOrigin covers that).
    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Location"));

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapWolverineEndpoints();

return await app.RunJasperFxCommands(args);
