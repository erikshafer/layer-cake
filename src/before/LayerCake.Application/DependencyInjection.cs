using System.Reflection;
using FluentValidation;
using LayerCake.Application.Common.Behaviors;
using LayerCake.Application.Common.Interfaces;
using LayerCake.Application.Coupons;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LayerCake.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<ICouponValidationService, CouponValidationService>();

        return services;
    }
}
