using FluentAssertions;
using Moq;
using StoreOps.Application.Alerts;
using StoreOps.Application.Alerts.Errors;
using StoreOps.Domain.Alerts;

namespace StoreOps.Application.Tests.Alerts;

public sealed class AlertsServiceTests
{
    private readonly Mock<INotificationRepository> _repoMock = new();

    private AlertsService CreateSut() => new(_repoMock.Object);

    [Fact]
    public async Task UpdateStatusAsync_WhenNotFound_ThrowsAlertNotFoundError()
    {
        var id = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        var sut = CreateSut();

        await sut.Invoking(s => s.UpdateStatusAsync(id, NotificationStatus.Read, CancellationToken.None))
            .Should().ThrowAsync<AlertNotFoundError>()
            .Where(e => e.Code == "ALERT_NOT_FOUND" && e.StatusCode == 404);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenFound_UpdatesStatusAndPersists()
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = NotificationStatus.Unread,
            AlertType = AlertType.Inventory,
            Channel = NotificationChannel.InApp,
            Message = "Low stock",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _repoMock
            .Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, CancellationToken _) => n);

        var sut = CreateSut();

        var result = await sut.UpdateStatusAsync(notification.Id, NotificationStatus.Acknowledged, CancellationToken.None);

        result.Status.Should().Be(NotificationStatus.Acknowledged);
        result.AcknowledgedAt.Should().NotBeNull();

        _repoMock.Verify(r => r.UpdateAsync(It.Is<Notification>(n =>
            n.Status == NotificationStatus.Acknowledged), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyMessage_ThrowsAlertValidationError()
    {
        var request = new CreateAlertRequest
        {
            UserId = Guid.NewGuid(),
            Message = string.Empty,
        };

        var sut = CreateSut();

        await sut.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<AlertValidationError>()
            .Where(e => e.Code == "ALERT_VALIDATION_ERROR");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsUnreadNotification()
    {
        var request = new CreateAlertRequest
        {
            UserId = Guid.NewGuid(),
            AlertType = AlertType.SlaBreach,
            Channel = NotificationChannel.InApp,
            Message = "Task overdue",
        };

        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, CancellationToken _) => n);

        var sut = CreateSut();

        var result = await sut.CreateAsync(request, CancellationToken.None);

        result.Status.Should().Be(NotificationStatus.Unread);
        result.Message.Should().Be("Task overdue");
        result.AlertType.Should().Be(AlertType.SlaBreach);
    }
}
