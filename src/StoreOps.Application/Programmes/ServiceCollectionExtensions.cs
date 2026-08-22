using Microsoft.Extensions.DependencyInjection;

namespace StoreOps.Application.Programmes;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProgrammesModule(this IServiceCollection services)
    {
        services.AddScoped<IProgrammesService, ProgrammesService>();
        return services;
    }
}
