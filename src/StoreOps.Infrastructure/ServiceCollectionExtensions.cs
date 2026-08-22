using Microsoft.Extensions.DependencyInjection;
using StoreOps.Application.Activities;
using StoreOps.Application.Alerts;
using StoreOps.Application.Common;
using StoreOps.Application.Programmes;
using StoreOps.Application.Reports;
using StoreOps.Application.Staff;
using StoreOps.Infrastructure.Activities;
using StoreOps.Infrastructure.Alerts;
using StoreOps.Infrastructure.EventBus;
using StoreOps.Infrastructure.Programmes;
using StoreOps.Infrastructure.Reports;
using StoreOps.Infrastructure.Staff;

namespace StoreOps.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
        services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<INotificationRepository, InMemoryNotificationRepository>();
        services.AddSingleton<IReportRepository, InMemoryReportRepository>();

        return services;
    }
}
