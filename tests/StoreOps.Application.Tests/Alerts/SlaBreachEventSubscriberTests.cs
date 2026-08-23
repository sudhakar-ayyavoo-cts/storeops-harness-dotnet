using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StoreOps.Application.Alerts;
using StoreOps.Application.Common;
using StoreOps.Domain.Alerts;
using StoreOps.Domain.Events;

namespace StoreOps.Application.Tests.Alerts;

public sealed class SlaBreachEventSubscriberTests
{
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly Mock<IAlertsService> _alertsServiceMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private Func<SlaBreachEvent, Task>? _capturedHandler;

    public SlaBreachEventSubscriberTests()
    {
        _eventBusMock
            .Setup(b => b.Subscribe(It.IsAny<Func<SlaBreachEvent, Task>>()))
            .Callback<Func<SlaBreachEvent, Task>>(h => _capturedHandler = h);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(p => p.GetService(typeof(IAlertsService)))
            .Returns(_alertsServiceMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
    }

    [Fact]
    public async Task HandleAsync_OnSlaBreachEvent_CreatesSlaBreachNotificationForDepartmentLead()
    {
        var sut = new SlaBreachEventSubscriber(_eventBusMock.Object, _scopeFactoryMock.Object);
        await sut.StartAsync(CancellationToken.None);

        _capturedHandler.Should().NotBeNull("StartAsync must subscribe a handler to SlaBreachEvent");

        var taskId = Guid.NewGuid();
        var departmentLeadId = Guid.NewGuid();
        var breachedAt = DateTimeOffset.UtcNow;
        var domainEvent = new SlaBreachEvent(taskId, Guid.NewGuid(), departmentLeadId, breachedAt);

        await _capturedHandler!(domainEvent);

        _alertsServiceMock.Verify(s => s.CreateAsync(
            It.Is<CreateAlertRequest>(r =>
                r.UserId == departmentLeadId &&
                r.AlertType == AlertType.SlaBreach &&
                r.Channel == NotificationChannel.InApp &&
                r.RelatedEntityId == taskId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
