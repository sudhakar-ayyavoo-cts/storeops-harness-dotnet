using FluentAssertions;
using Moq;
using StoreOps.Application.Activities;
using StoreOps.Application.Common;
using StoreOps.Application.Staff;
using StoreOps.Domain.Activities;
using StoreOps.Domain.Events;
using StoreOps.Domain.Staff;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Application.Tests.Activities;

public sealed class SlaSweepServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITaskRepository> _taskRepoMock = new();
    private readonly Mock<IStaffService> _staffServiceMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly Mock<IClock> _clockMock = new();

    public SlaSweepServiceTests()
    {
        _clockMock.SetupGet(c => c.UtcNow).Returns(Now);
        _taskRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<StoreTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoreTask t, CancellationToken _) => t);
    }

    private SlaSweepService CreateSut() =>
        new(_taskRepoMock.Object, _staffServiceMock.Object, _eventBusMock.Object, _clockMock.Object);

    private static StoreTask MakeTask(
        Guid storeId,
        TaskPriority priority,
        DomainTaskStatus status,
        DateTimeOffset? dueDate,
        Guid? assignedToUserId = null,
        DateTimeOffset? slaBreachedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Test task",
        Priority = priority,
        Status = status,
        Category = TaskCategory.General,
        StoreId = storeId,
        AssignedToUserId = assignedToUserId,
        DueDate = dueDate,
        SlaBreachedAt = slaBreachedAt,
        CreatedAt = Now.AddDays(-3),
        UpdatedAt = Now.AddDays(-3),
    };

    private static User MakeUser(Guid storeId, StaffRole role, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        Email = $"{Guid.NewGuid()}@storeops.demo",
        Role = role,
        StoreId = storeId,
        CreatedAt = createdAt,
    };

    private void SetupTasks(params StoreTask[] tasks) =>
        _taskRepoMock
            .Setup(r => r.ListAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StoreTask>)tasks);

    private void SetupStaff(Guid storeId, params User[] staff) =>
        _staffServiceMock
            .Setup(s => s.ListAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<User>)staff);

    [Fact]
    public async Task SweepAsync_CriticalOverdueTaskWithDepartmentLead_PublishesSlaBreachEvent()
    {
        var storeId = Guid.NewGuid();
        var assignee = Guid.NewGuid();
        var departmentLead = MakeUser(storeId, StaffRole.DepartmentLead, Now.AddMonths(-1));
        var task = MakeTask(storeId, TaskPriority.Critical, DomainTaskStatus.InProgress, Now.AddHours(-2), assignee);

        SetupTasks(task);
        SetupStaff(storeId, departmentLead);

        var sut = CreateSut();
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(1);

        _eventBusMock.Verify(b => b.Publish(It.Is<SlaBreachEvent>(e =>
            e.TaskId == task.Id &&
            e.AssignedToUserId == assignee &&
            e.DepartmentLeadId == departmentLead.Id &&
            e.BreachedAt == Now)), Times.Once);

        _taskRepoMock.Verify(r => r.UpdateAsync(
            It.Is<StoreTask>(t => t.Id == task.Id && t.SlaBreachedAt == Now),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SweepAsync_HighPriorityOverdueTask_PublishesSlaBreachEvent()
    {
        var storeId = Guid.NewGuid();
        var departmentLead = MakeUser(storeId, StaffRole.DepartmentLead, Now.AddMonths(-1));
        var task = MakeTask(storeId, TaskPriority.High, DomainTaskStatus.InProgress, Now.AddHours(-2), Guid.NewGuid());

        SetupTasks(task);
        SetupStaff(storeId, departmentLead);

        var sut = CreateSut();
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(1);
        _eventBusMock.Verify(b => b.Publish(It.Is<SlaBreachEvent>(e => e.TaskId == task.Id)), Times.Once);
    }

    [Fact]
    public async Task SweepAsync_RunTwice_DoesNotPublishSecondEventForAlreadyBreachedTask()
    {
        var storeId = Guid.NewGuid();
        var departmentLead = MakeUser(storeId, StaffRole.DepartmentLead, Now.AddMonths(-1));
        var task = MakeTask(
            storeId,
            TaskPriority.Critical,
            DomainTaskStatus.InProgress,
            Now.AddHours(-2),
            Guid.NewGuid(),
            slaBreachedAt: Now.AddHours(-1));

        SetupTasks(task);
        SetupStaff(storeId, departmentLead);

        var sut = CreateSut();
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(0);
        _eventBusMock.Verify(b => b.Publish(It.Is<SlaBreachEvent>(e => e.TaskId == task.Id)), Times.Never);
    }

    [Fact]
    public async Task SweepAsync_NotYetDue_DoesNotPublish()
    {
        var storeId = Guid.NewGuid();
        var task = MakeTask(storeId, TaskPriority.Critical, DomainTaskStatus.Todo, Now.AddDays(1));

        SetupTasks(task);

        var sut = CreateSut();
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(0);
        task.SlaBreachedAt.Should().BeNull();
        _eventBusMock.Verify(b => b.Publish(It.IsAny<SlaBreachEvent>()), Times.Never);
    }

    [Fact]
    public async Task SweepAsync_TaskAlreadyDone_DoesNotPublish()
    {
        var storeId = Guid.NewGuid();
        var task = MakeTask(storeId, TaskPriority.Critical, DomainTaskStatus.Done, Now.AddDays(-1));

        SetupTasks(task);

        var sut = CreateSut();
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(0);
        _eventBusMock.Verify(b => b.Publish(It.IsAny<SlaBreachEvent>()), Times.Never);
    }

    [Fact]
    public async Task SweepAsync_NoDepartmentLeadInStore_SkipsTaskButProcessesOthers()
    {
        var storeWithoutLead = Guid.NewGuid();
        var storeWithLead = Guid.NewGuid();
        var departmentLead = MakeUser(storeWithLead, StaffRole.DepartmentLead, Now.AddMonths(-1));

        var unresolvableTask = MakeTask(storeWithoutLead, TaskPriority.Critical, DomainTaskStatus.InProgress, Now.AddHours(-1));
        var resolvableTask = MakeTask(storeWithLead, TaskPriority.Critical, DomainTaskStatus.InProgress, Now.AddHours(-1));

        SetupTasks(unresolvableTask, resolvableTask);
        SetupStaff(storeWithoutLead);
        SetupStaff(storeWithLead, departmentLead);

        var sut = CreateSut();
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(1);
        _eventBusMock.Verify(b => b.Publish(It.Is<SlaBreachEvent>(e => e.TaskId == unresolvableTask.Id)), Times.Never);
        _eventBusMock.Verify(b => b.Publish(It.Is<SlaBreachEvent>(e => e.TaskId == resolvableTask.Id)), Times.Once);
    }

    [Fact]
    public async Task SweepAsync_UnassignedTask_PublishesEventWithEmptyAssignee()
    {
        var storeId = Guid.NewGuid();
        var departmentLead = MakeUser(storeId, StaffRole.DepartmentLead, Now.AddMonths(-1));
        var task = MakeTask(storeId, TaskPriority.Critical, DomainTaskStatus.InProgress, Now.AddHours(-1), assignedToUserId: null);

        SetupTasks(task);
        SetupStaff(storeId, departmentLead);

        var sut = CreateSut();
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(1);
        _eventBusMock.Verify(b => b.Publish(It.Is<SlaBreachEvent>(e =>
            e.TaskId == task.Id &&
            e.AssignedToUserId == Guid.Empty &&
            e.DepartmentLeadId == departmentLead.Id)), Times.Once);
    }
}
