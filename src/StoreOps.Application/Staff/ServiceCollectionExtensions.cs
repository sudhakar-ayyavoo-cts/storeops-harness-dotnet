using Microsoft.Extensions.DependencyInjection;

namespace StoreOps.Application.Staff;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStaffModule(this IServiceCollection services)
    {
        services.AddScoped<IStaffService, StaffService>();
        return services;
    }
}
