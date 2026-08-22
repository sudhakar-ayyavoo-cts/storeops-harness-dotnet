using Microsoft.Extensions.DependencyInjection;

namespace StoreOps.Application.Alerts;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlertsModule(this IServiceCollection services)
    {
        services.AddScoped<IAlertsService, AlertsService>();
        return services;
    }
}
