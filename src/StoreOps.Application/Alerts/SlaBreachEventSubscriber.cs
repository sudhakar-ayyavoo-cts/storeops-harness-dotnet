using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StoreOps.Application.Common;
using StoreOps.Domain.Alerts;
using StoreOps.Domain.Events;

namespace StoreOps.Application.Alerts;

public sealed class SlaBreachEventSubscriber : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly IServiceScopeFactory _scopeFactory;

    public SlaBreachEventSubscriber(IEventBus eventBus, IServiceScopeFactory scopeFactory)
    {
        _eventBus = eventBus;
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _eventBus.Subscribe<SlaBreachEvent>(HandleAsync);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task HandleAsync(SlaBreachEvent e)
    {
        using var scope = _scopeFactory.CreateScope();
        var alertsService = scope.ServiceProvider.GetRequiredService<IAlertsService>();

        await alertsService.CreateAsync(new CreateAlertRequest
        {
            UserId = e.DepartmentLeadId,
            AlertType = AlertType.SlaBreach,
            Channel = NotificationChannel.InApp,
            Message = $"Task {e.TaskId} breached its SLA at {e.BreachedAt:O} and needs attention.",
            RelatedEntityId = e.TaskId,
        }, CancellationToken.None);
    }
}
