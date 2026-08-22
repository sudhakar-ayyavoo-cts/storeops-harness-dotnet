using FluentAssertions;
using Moq;
using StoreOps.Application.Activities;
using StoreOps.Application.Activities.Errors;
using StoreOps.Application.Staff;
using StoreOps.Domain.Activities;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Application.Tests.Activities;

public sealed class ActivitiesServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepoMock = new();
    private readonly Mock<IStaffService> _staffServiceMock = new();

    private ActivitiesService CreateSut() =>
        new(_taskRepoMock.Object, _staffServiceMock.Object);

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsTaskWithTodoStatus()
    {
        var request = new CreateTaskRequest
        {
            Title = "Restock aisle 3",
            StoreId = Guid.NewGuid(),
            Priority = TaskPriority.High,
            Category = TaskCategory.Restocking,
        };

        _taskRepoMock
            .Setup(r => r.AddAsync(It.IsAny<StoreTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreTask t, CancellationToken _) => t);

        var sut = CreateSut();

        var result = await sut.CreateAsync(request, CancellationToken.None);

        result.Status.Should().Be(DomainTaskStatus.Todo);
        result.Title.Should().Be("Restock aisle 3");
        result.Priority.Should().Be(TaskPriority.High);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithEmptyTitle_ThrowsTaskValidationError()
    {
        var request = new CreateTaskRequest
        {
            Title = string.Empty,
            StoreId = Guid.NewGuid(),
        };

        var sut = CreateSut();

        await sut.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<TaskValidationError>()
            .Where(e => e.Code == "TASK_VALIDATION_ERROR");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyStoreId_ThrowsTaskValidationError()
    {
        var request = new CreateTaskRequest
        {
            Title = "Valid title",
            StoreId = Guid.Empty,
        };

        var sut = CreateSut();

        await sut.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<TaskValidationError>();
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentAssignee_ThrowsTaskValidationError()
    {
        var assigneeId = Guid.NewGuid();
        var request = new CreateTaskRequest
        {
            Title = "Valid title",
            StoreId = Guid.NewGuid(),
            AssignedToUserId = assigneeId,
        };

        _staffServiceMock
            .Setup(s => s.GetByIdAsync(assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Staff.User?)null);

        var sut = CreateSut();

        await sut.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<TaskValidationError>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskNotFound_ThrowsTaskNotFoundError()
    {
        var taskId = Guid.NewGuid();

        _taskRepoMock
            .Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreTask?)null);

        var sut = CreateSut();

        await sut.Invoking(s => s.GetByIdAsync(taskId, CancellationToken.None))
            .Should().ThrowAsync<TaskNotFoundError>()
            .Where(e => e.Code == "TASK_NOT_FOUND" && e.StatusCode == 404);
    }

    [Fact]
    public async Task ListAsync_DelegatesToRepository()
    {
        var storeId = Guid.NewGuid();
        var expectedTasks = new List<StoreTask>
        {
            new() { Id = Guid.NewGuid(), Title = "Task A", StoreId = storeId, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
        };

        _taskRepoMock
            .Setup(r => r.ListAsync(null, storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTasks);

        var sut = CreateSut();

        var result = await sut.ListAsync(null, storeId, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Task A");
    }
}
