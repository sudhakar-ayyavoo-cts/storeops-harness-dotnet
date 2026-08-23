using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StoreOps.Api.Contracts.Activities;
using StoreOps.Api.Contracts.Alerts;
using StoreOps.Application.Alerts;
using StoreOps.Domain.Activities;
using StoreOps.Domain.Alerts;
using StoreOps.Domain.Staff;

namespace StoreOps.Api.Tests.Alerts;

public sealed class SlaEscalationSweepEndpointTests : IClassFixture<StoreOpsWebFactory>
{
    private readonly HttpClient _client;
    private readonly IServiceProvider _services;

    public SlaEscalationSweepEndpointTests(StoreOpsWebFactory factory)
    {
        _client = factory.CreateClient();
        _services = factory.Services;
    }

    [Fact]
    public async Task Post_SlaEscalationSweep_EscalatesUnresolvedBreachToStoreManager()
    {
        var storeId = Guid.NewGuid();

        var userRepository = _services.GetRequiredService<StoreOps.Application.Staff.IUserRepository>();
        var storeManager = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@storeops.demo",
            PasswordHash = "irrelevant",
            Role = StaffRole.StoreManager,
            StoreId = storeId,
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-1),
        };
        await userRepository.AddAsync(storeManager, CancellationToken.None);

        var createTaskDto = new CreateTaskRequestDto
        {
            Title = "Unresolved task for escalation test",
            Priority = TaskPriority.Critical,
            Category = TaskCategory.Compliance,
            StoreId = storeId,
        };
        var createTaskResponse = await _client.PostAsJsonAsync("/api/tasks", createTaskDto);
        createTaskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await createTaskResponse.Content.ReadFromJsonAsync<TaskDto>();

        var notificationRepository = _services.GetRequiredService<INotificationRepository>();
        await notificationRepository.AddAsync(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AlertType = AlertType.SlaBreach,
            Channel = NotificationChannel.InApp,
            Status = NotificationStatus.Unread,
            Message = "SLA breach for escalation test",
            RelatedEntityId = task!.Id,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-6),
        }, CancellationToken.None);

        var sweepResponse = await _client.PostAsync("/api/alerts/sla-escalation-sweep", content: null);

        sweepResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var sweepResult = await sweepResponse.Content.ReadFromJsonAsync<SlaEscalationSweepResultDto>();
        sweepResult.Should().NotBeNull();
        sweepResult!.EscalationsCreated.Should().BeGreaterThanOrEqualTo(1);

        var alertsResponse = await _client.GetAsync($"/api/alerts?userId={storeManager.Id}");
        alertsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var alerts = await alertsResponse.Content.ReadFromJsonAsync<List<NotificationDto>>();
        alerts.Should().Contain(n => n.AlertType == AlertType.Escalation && n.RelatedEntityId == task.Id);
    }
}
