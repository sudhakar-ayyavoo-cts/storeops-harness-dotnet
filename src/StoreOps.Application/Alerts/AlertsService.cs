using StoreOps.Application.Alerts.Errors;
using StoreOps.Domain.Alerts;

namespace StoreOps.Application.Alerts;

public sealed class AlertsService : IAlertsService
{
    private readonly INotificationRepository _notificationRepository;

    public AlertsService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IReadOnlyList<Notification>> ListAsync(
        Guid? userId,
        Guid? storeId,
        CancellationToken ct)
        => await _notificationRepository.ListAsync(userId, storeId, ct);

    public async Task<Notification> UpdateStatusAsync(
        Guid id,
        NotificationStatus status,
        CancellationToken ct)
    {
        var notification = await _notificationRepository.GetByIdAsync(id, ct);
        if (notification is null)
        {
            throw new AlertNotFoundError(id);
        }

        notification.Status = status;
        if (status == NotificationStatus.Acknowledged)
        {
            notification.AcknowledgedAt = DateTimeOffset.UtcNow;
        }

        return await _notificationRepository.UpdateAsync(notification, ct);
    }

    public async Task<Notification> CreateAsync(CreateAlertRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new AlertValidationError("Message is required.");
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            AlertType = request.AlertType,
            Channel = request.Channel,
            Status = NotificationStatus.Unread,
            Message = request.Message,
            RelatedEntityId = request.RelatedEntityId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        return await _notificationRepository.AddAsync(notification, ct);
    }
}
