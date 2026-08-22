using Microsoft.Extensions.DependencyInjection;

namespace StoreOps.Application.Reports;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        services.AddScoped<IReportsService, ReportsService>();
        return services;
    }
}
