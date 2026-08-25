using JasperFx;
using LayerCake.Slices;
using Marten;
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

builder.Services.AddMarten(opts =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=layercake;Username=postgres;Password=postgres";

    opts.Connection(connectionString);

    // The "after" schema: both twins share one PostgreSQL database without
    // touching each other. The database engine never changes; only the idiom.
    opts.DatabaseSchemaName = "after";

    // Explicit camelCase for the stored JSON so streamed Marten results
    // (Marten.AspNetCore) match the wire contract without re-serializing.
    opts.UseSystemTextJsonForSerialization(casing: Casing.CamelCase);
})
.IntegrateWithWolverine()
.UseLightweightSessions();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(AfterTwin).Assembly);
    opts.Policies.AutoApplyTransactions();
    opts.ServiceName = "LayerCake";

    // CritterWatch monitoring is opt-in via launchSettings (the live-demo run).
    // The contract tests boot this host through Alba without the flag, so
    // `dotnet test` never needs RabbitMQ or the console running.
    if (builder.Configuration.GetValue<bool>("CritterWatch:Enabled"))
    {
        // Telemetry channel only; no conventional routing, so no accidental
        // message surface appears on the broker.
        opts.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("rabbitmq") ?? "amqp://localhost"))
            .AutoProvision();

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
    app.UseSwagger();
    app.UseSwaggerUI();

    await SeedData.ApplyAsync(app.Services.GetRequiredService<IDocumentStore>());
}

app.MapWolverineEndpoints();

return await app.RunJasperFxCommands(args);
