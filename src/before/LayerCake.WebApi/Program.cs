using LayerCake.Application;
using LayerCake.Infrastructure;
using LayerCake.Infrastructure.Persistence;
using LayerCake.WebApi.Filters;
using LayerCake.WebApi.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilterAttribute>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCors();

// The RabbitMQ consumer for the baker notification runs inside this host.
builder.Services.AddHostedService<NotifyBakerConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Development-only CORS for the static demo page in src/frontend/ (opened
    // from disk, so its origin is "null"; AllowAnyOrigin covers that).
    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Location"));

    app.UseSwagger();
    app.UseSwaggerUI();

    // Migrations, not EnsureCreated: the earnest choice (see docs/build-log.md).
    // Applied at startup so "docker compose up -d" + run is the whole ritual.
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LayerCakeDbContext>();
    await dbContext.Database.MigrateAsync();
    await LayerCakeDbContextSeeder.SeedAsync(dbContext);
}

app.MapControllers();

await app.RunAsync();
