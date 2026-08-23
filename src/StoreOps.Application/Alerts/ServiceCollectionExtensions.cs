using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StoreOps.Application.Alerts;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlertsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AlertsOptions>(configuration.GetSection("Alerts"));

        services.AddScoped<IAlertsService, AlertsService>();
        services.AddScoped<IAlertsEscalationSweepService, AlertsEscalationSweepService>();
        services.AddHostedService<SlaBreachEventSubscriber>();

        return services;
    }
}
