using LayerCake.Application.Common.Interfaces;
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

        return services;
    }
}
