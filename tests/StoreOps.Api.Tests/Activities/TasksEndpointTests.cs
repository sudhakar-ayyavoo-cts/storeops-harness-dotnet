using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StoreOps.Api.Contracts.Activities;
using StoreOps.Domain.Activities;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Api.Tests.Activities;

public sealed class TasksEndpointTests : IClassFixture<StoreOpsWebFactory>
{
    private readonly HttpClient _client;

    public TasksEndpointTests(StoreOpsWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Tasks_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_WithValidRequest_CreatesTaskAndReturnsCreated()
    {
        var storeId = Guid.NewGuid();
        var dto = new CreateTaskRequestDto
        {
            Title = "Integration test task",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.General,
            StoreId = storeId,
        };

        var response = await _client.PostAsJsonAsync("/api/tasks", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TaskDto>();
        body.Should().NotBeNull();
        body!.Title.Should().Be("Integration test task");
        body.Status.Should().Be(DomainTaskStatus.Todo);
        body.StoreId.Should().Be(storeId);
        body.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Post_WithEmptyTitle_Returns422()
    {
        var dto = new CreateTaskRequestDto
        {
            Title = string.Empty,
            StoreId = Guid.NewGuid(),
        };

        var response = await _client.PostAsJsonAsync("/api/tasks", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("TASK_VALIDATION_ERROR");
    }

    [Fact]
    public async Task Get_Tasks_FilteredByStatus_ReturnsOnlyMatchingTasks()
    {
        var storeId = Guid.NewGuid();

        var createDto = new CreateTaskRequestDto
        {
            Title = "Status-filter test",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            StoreId = storeId,
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>();

        var response = await _client.GetAsync($"/api/tasks?status=Todo&storeId={storeId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();
        tasks.Should().NotBeNull();
        tasks!.Should().Contain(t => t.Id == created!.Id && t.Status == DomainTaskStatus.Todo);
    }
}
