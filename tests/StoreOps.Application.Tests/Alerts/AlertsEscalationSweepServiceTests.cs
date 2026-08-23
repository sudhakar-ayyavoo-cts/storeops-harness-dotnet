using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using StoreOps.Application.Activities;
using StoreOps.Application.Alerts;
using StoreOps.Application.Common;
using StoreOps.Application.Staff;
using StoreOps.Domain.Activities;
using StoreOps.Domain.Alerts;
using StoreOps.Domain.Staff;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Application.Tests.Alerts;

public sealed class AlertsEscalationSweepServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<INotificationRepository> _notificationRepoMock = new();
    private readonly Mock<IActivitiesService> _activitiesServiceMock = new();
    private readonly Mock<IStaffService> _staffServiceMock = new();
    private readonly Mock<IClock> _clockMock = new();

    public AlertsEscalationSweepServiceTests()
    {
        _clockMock.SetupGet(c => c.UtcNow).Returns(Now);
        _notificationRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, CancellationToken _) => n);
    }

    private AlertsEscalationSweepService CreateSut(int graceHours = 4) => new(
        _notificationRepoMock.Object,
        _activitiesServiceMock.Object,
        _staffServiceMock.Object,
        _clockMock.Object,
        Options.Create(new AlertsOptions { SlaEscalationGraceHours = graceHours }));

    private static Notification MakeSlaBreachNotification(
        Guid taskId,
        DateTimeOffset createdAt,
        NotificationStatus status = NotificationStatus.Unread) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        AlertType = AlertType.SlaBreach,
        Channel = NotificationChannel.InApp,
        Status = status,
        Message = "SLA breach",
        RelatedEntityId = taskId,
        CreatedAt = createdAt,
    };

    private static StoreTask MakeTask(Guid taskId, Guid storeId, DomainTaskStatus status) => new()
    {
        Id = taskId,
        Title = "Test task",
        Status = status,
        Priority = TaskPriority.Critical,
        Category = TaskCategory.General,
        StoreId = storeId,
        CreatedAt = Now.AddDays(-3),
        UpdatedAt = Now.AddDays(-3),
    };

    private static User MakeStoreManager(Guid storeId, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        Email = $"{Guid.NewGuid()}@storeops.demo",
        Role = StaffRole.StoreManager,
        StoreId = storeId,
        CreatedAt = createdAt,
    };

    private void SetupAllNotifications(params Notification[] notifications) =>
        _notificationRepoMock
            .Setup(r => r.ListAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Notification>)notifications);

    [Fact]
    public async Task SweepAsync_UnresolvedBreachPastGracePeriod_CreatesEscalationForStoreManager()
    {
        var taskId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var breachNotification = MakeSlaBreachNotification(taskId, Now.AddHours(-5));
        var storeManager = MakeStoreManager(storeId, Now.AddMonths(-1));

        SetupAllNotifications(breachNotification);
        _activitiesServiceMock
            .Setup(s => s.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTask(taskId, storeId, DomainTaskStatus.InProgress));
        _staffServiceMock
            .Setup(s => s.ListAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { storeManager });

        var sut = CreateSut(graceHours: 4);
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(1);
        _notificationRepoMock.Verify(r => r.AddAsync(
            It.Is<Notification>(n =>
                n.UserId == storeManager.Id &&
                n.AlertType == AlertType.Escalation &&
                n.RelatedEntityId == taskId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SweepAsync_GracePeriodNotYetElapsed_DoesNotEscalate()
    {
        var taskId = Guid.NewGuid();
        var breachNotification = MakeSlaBreachNotification(taskId, Now.AddHours(-3));

        SetupAllNotifications(breachNotification);

        var sut = CreateSut(graceHours: 4);
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(0);
        _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SweepAsync_TaskAlreadyDone_DoesNotEscalate()
    {
        var taskId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var breachNotification = MakeSlaBreachNotification(taskId, Now.AddHours(-5));

        SetupAllNotifications(breachNotification);
        _activitiesServiceMock
            .Setup(s => s.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTask(taskId, storeId, DomainTaskStatus.Done));

        var sut = CreateSut(graceHours: 4);
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(0);
        _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SweepAsync_BreachAlreadyAcknowledged_DoesNotEscalate()
    {
        var taskId = Guid.NewGuid();
        var breachNotification = MakeSlaBreachNotification(taskId, Now.AddHours(-5), NotificationStatus.Acknowledged);

        SetupAllNotifications(breachNotification);

        var sut = CreateSut(graceHours: 4);
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(0);
        _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
        _activitiesServiceMock.Verify(s => s.GetByIdAsync(taskId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SweepAsync_AlreadyEscalated_DoesNotCreateDuplicateEscalation()
    {
        var taskId = Guid.NewGuid();
        var breachNotification = MakeSlaBreachNotification(taskId, Now.AddHours(-5));
        var existingEscalation = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AlertType = AlertType.Escalation,
            Channel = NotificationChannel.InApp,
            Status = NotificationStatus.Unread,
            Message = "already escalated",
            RelatedEntityId = taskId,
            CreatedAt = Now.AddHours(-1),
        };

        SetupAllNotifications(breachNotification, existingEscalation);

        var sut = CreateSut(graceHours: 4);
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(0);
        _notificationRepoMock.Verify(r => r.AddAsync(
            It.Is<Notification>(n => n.AlertType == AlertType.Escalation),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SweepAsync_NoStoreManagerInStore_SkipsButProcessesOtherCandidates()
    {
        var taskWithoutManager = Guid.NewGuid();
        var storeWithoutManager = Guid.NewGuid();
        var taskWithManager = Guid.NewGuid();
        var storeWithManager = Guid.NewGuid();

        var breach1 = MakeSlaBreachNotification(taskWithoutManager, Now.AddHours(-5));
        var breach2 = MakeSlaBreachNotification(taskWithManager, Now.AddHours(-5));
        var storeManager = MakeStoreManager(storeWithManager, Now.AddMonths(-1));

        SetupAllNotifications(breach1, breach2);
        _activitiesServiceMock
            .Setup(s => s.GetByIdAsync(taskWithoutManager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTask(taskWithoutManager, storeWithoutManager, DomainTaskStatus.InProgress));
        _activitiesServiceMock
            .Setup(s => s.GetByIdAsync(taskWithManager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTask(taskWithManager, storeWithManager, DomainTaskStatus.InProgress));
        _staffServiceMock
            .Setup(s => s.ListAsync(storeWithoutManager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());
        _staffServiceMock
            .Setup(s => s.ListAsync(storeWithManager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { storeManager });

        var sut = CreateSut(graceHours: 4);
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(1);
        _notificationRepoMock.Verify(r => r.AddAsync(
            It.Is<Notification>(n => n.RelatedEntityId == taskWithoutManager),
            It.IsAny<CancellationToken>()), Times.Never);
        _notificationRepoMock.Verify(r => r.AddAsync(
            It.Is<Notification>(n => n.RelatedEntityId == taskWithManager),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SweepAsync_GracePeriodIsConfigurable_ShorterGraceEscalatesSooner()
    {
        var taskId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var breachNotification = MakeSlaBreachNotification(taskId, Now.AddMinutes(-90));
        var storeManager = MakeStoreManager(storeId, Now.AddMonths(-1));

        SetupAllNotifications(breachNotification);
        _activitiesServiceMock
            .Setup(s => s.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeTask(taskId, storeId, DomainTaskStatus.InProgress));
        _staffServiceMock
            .Setup(s => s.ListAsync(storeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { storeManager });

        var sut = CreateSut(graceHours: 1);
        var result = await sut.SweepAsync(CancellationToken.None);

        result.Should().Be(1, "90 minutes exceeds a configured 1-hour grace period");
    }
}
