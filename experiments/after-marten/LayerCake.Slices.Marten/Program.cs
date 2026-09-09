using JasperFx;
using LayerCake.Slices;
using LayerCake.Slices.Cakes;
using LayerCake.Slices.Orders;
using Marten;
using Marten.Schema;
using Weasel.Core;
using Wolverine;
using Wolverine.CritterWatch;
using Wolverine.Http;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddWolverineHttp();
builder.Services.AddCors();

builder.Services.AddMarten(opts =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=layercake;Username=postgres;Password=postgres";

    opts.Connection(connectionString);

    // The "after" schema: both twins share one PostgreSQL database without
    // touching each other. The database engine never changes; only the idiom.
    opts.DatabaseSchemaName = "after";

    // Explicit camelCase for the stored JSON so what lands in mt_doc_* reads
    // the same as the wire contract (the JSON is exhibit material on a slide).
    opts.UseSystemTextJsonForSerialization(casing: Casing.CamelCase);

    // The before twin enforces cake-name uniqueness with an EF Core unique
    // index; this is the Marten counterpart so the 409 guard in PublishCake is
    // backed by the database on both twins, not just a check-then-insert.
    opts.Schema.For<Cake>().UniqueIndex(UniqueIndexType.Computed, x => x.Name);
})
.IntegrateWithWolverine()
.UseLightweightSessions();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(AfterTwin).Assembly);
    opts.Policies.AutoApplyTransactions();
    opts.ServiceName = "LayerCake";

    // The broker is part of the app now, not just the telemetry channel.
    // AutoProvision declares the queue on startup; the contract suite points
    // this at its own Testcontainers broker through the same setting.
    opts.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("rabbitmq") ?? "amqp://localhost"))
        .AutoProvision();

    // The cascaded NotifyBaker goes to RabbitMQ. UseDurableOutbox keeps it an
    // outbox message: the envelope is written to the Wolverine table in the
    // SAME Marten transaction as the order and sent to the broker after the
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Development-only CORS for the static demo page in src/frontend/ (opened
    // from disk, so its origin is "null"; AllowAnyOrigin covers that).
    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Location"));

    app.UseSwagger();
    app.UseSwaggerUI();

    await SeedData.ApplyAsync(app.Services.GetRequiredService<IDocumentStore>());
}

app.MapWolverineEndpoints();

return await app.RunJasperFxCommands(args);
