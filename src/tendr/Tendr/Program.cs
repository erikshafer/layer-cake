using JasperFx;
using JasperFx.Events.Projections;
using JasperFx.Resources;
using Marten;
using Tendr;
using Tendr.Authorizations;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddWolverineHttp();

builder.Services.AddMarten(opts =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=layercake;Username=postgres;Password=postgres";

    opts.Connection(connectionString);

    // A schema in the bakery's database, which is a demo convenience and
    // nothing more: Tendr never reads "before" or "after", and pointing it at
    // its own server is this one connection string.
    opts.DatabaseSchemaName = "tendr";

    // The events are the record. The Authorization snapshot is updated in the
    // same transaction as the append, so the GET and the replay check are
    // plain document loads.
    opts.Projections.Snapshot<Authorization>(SnapshotLifecycle.Inline);
})
.IntegrateWithWolverine()
.UseLightweightSessions();

// Marten builds the tendr schema, and Wolverine its envelope tables, as the
// host starts. Tendr starts empty; there is no seed data.
builder.Services.AddResourceSetupOnStartup();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(TendrApi).Assembly);
    opts.ServiceName = "Tendr";
    opts.Policies.AutoApplyTransactions();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapWolverineEndpoints();

return await app.RunJasperFxCommands(args);
