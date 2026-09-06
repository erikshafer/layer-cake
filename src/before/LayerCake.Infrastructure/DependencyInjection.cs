using LayerCake.Application.Common.Interfaces;
using LayerCake.Infrastructure.Messaging;
using LayerCake.Infrastructure.Persistence;
using LayerCake.Infrastructure.Repositories;
using LayerCake.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LayerCake.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=layercake;Username=postgres;Password=postgres";

        services.AddDbContext<LayerCakeDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<ICakeRepository, CakeRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IBakerTaskRepository, BakerTaskRepository>();

        // The DbContext IS the unit of work; resolve the same scoped instance
        // the repositories stage their changes into.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LayerCakeDbContext>());

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Slice 004: the broker adapter behind IMessagePublisher. The queue
        // name comes from the RabbitMq section; the URI from ConnectionStrings
        // so it sits beside Postgres in appsettings and in the test fixture.
        // Bound by hand: the section binder lives in a package this project
        // does not reference, and the freeze allows no third addition.
        services.Configure<RabbitMqOptions>(options =>
        {
            options.ConnectionUri = configuration.GetConnectionString("RabbitMq") ?? options.ConnectionUri;
            options.QueueName = configuration[$"{RabbitMqOptions.SectionName}:QueueName"] ?? options.QueueName;
        });

        // One connection per host; the container disposes it with the host.
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        return services;
    }
}
