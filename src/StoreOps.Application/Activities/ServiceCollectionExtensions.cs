using Microsoft.Extensions.DependencyInjection;

namespace StoreOps.Application.Activities;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddActivitiesModule(this IServiceCollection services)
    {
        services.AddScoped<IActivitiesService, ActivitiesService>();
        return services;
    }
}
