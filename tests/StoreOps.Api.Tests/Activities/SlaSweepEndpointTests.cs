using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StoreOps.Api.Contracts.Activities;
using StoreOps.Domain.Activities;
using StoreOps.Domain.Staff;

namespace StoreOps.Api.Tests.Activities;

public sealed class SlaSweepEndpointTests : IClassFixture<StoreOpsWebFactory>
{
    private readonly HttpClient _client;
    private readonly IServiceProvider _services;

    public SlaSweepEndpointTests(StoreOpsWebFactory factory)
    {
        _client = factory.CreateClient();
        _services = factory.Services;
    }

    [Fact]
    public async Task Post_SlaSweep_DetectsOverdueCriticalTaskAndPersistsBreach()
    {
        var storeId = Guid.NewGuid();

        var userRepository = _services.GetRequiredService<StoreOps.Application.Staff.IUserRepository>();
        await userRepository.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@storeops.demo",
            PasswordHash = "irrelevant",
            Role = StaffRole.DepartmentLead,
            StoreId = storeId,
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-1),
        }, CancellationToken.None);

        var createDto = new CreateTaskRequestDto
        {
            Title = "Overdue critical task for sweep test",
            Priority = TaskPriority.Critical,
            Category = TaskCategory.Compliance,
            StoreId = storeId,
            DueDate = DateTimeOffset.UtcNow.AddHours(-2),
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();

        var sweepResponse = await _client.PostAsync("/api/tasks/sla-sweep", content: null);

        sweepResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var sweepResult = await sweepResponse.Content.ReadFromJsonAsync<SlaSweepResultDto>();
        sweepResult.Should().NotBeNull();
        sweepResult!.BreachesDetected.Should().BeGreaterThanOrEqualTo(1);

        var taskRepository = _services.GetRequiredService<StoreOps.Application.Activities.ITaskRepository>();
        var stored = await taskRepository.GetByIdAsync(created!.Id, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.SlaBreachedAt.Should().NotBeNull();
    }
}
