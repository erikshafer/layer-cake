using LayerCake.Application.Common.Interfaces;
using LayerCake.Infrastructure.Messaging;
using LayerCake.Infrastructure.Payments;
using LayerCake.Infrastructure.Persistence;
using LayerCake.Infrastructure.Repositories;
using LayerCake.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        // Bound by hand, key by key, so each setting's source reads here.
        services.Configure<RabbitMqOptions>(options =>
        {
            options.ConnectionUri = configuration.GetConnectionString("RabbitMq") ?? options.ConnectionUri;
            options.QueueName = configuration[$"{RabbitMqOptions.SectionName}:QueueName"] ?? options.QueueName;
        });

        // One connection per host; the container disposes it with the host.
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        // Slice 005: the card vendor adapter behind IPaymentGateway, bound by
        // hand like the broker settings. A typed client: IHttpClientFactory
        // hands TendrPaymentGateway an HttpClient with the base address and
        // timeout already set. The timeout is the whole resilience story; no
        // retry, no circuit breaker.
        services.Configure<TendrOptions>(options =>
        {
            options.BaseUrl = configuration[$"{TendrOptions.SectionName}:BaseUrl"] ?? options.BaseUrl;

            if (int.TryParse(configuration[$"{TendrOptions.SectionName}:TimeoutSeconds"], out var timeoutSeconds))
            {
                options.TimeoutSeconds = timeoutSeconds;
            }
        });

        services.AddHttpClient<IPaymentGateway, TendrPaymentGateway>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TendrOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
