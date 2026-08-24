using JasperFx;
using LayerCake.Slices;
using Marten;
using Weasel.Core;
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
