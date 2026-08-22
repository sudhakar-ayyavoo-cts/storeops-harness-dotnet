using StoreOps.Domain.Alerts;

namespace StoreOps.Application.Alerts;

public interface IAlertsService
{
    Task<IReadOnlyList<Notification>> ListAsync(Guid? userId, Guid? storeId, CancellationToken ct);
    Task<Notification> UpdateStatusAsync(Guid id, NotificationStatus status, CancellationToken ct);
    Task<Notification> CreateAsync(CreateAlertRequest request, CancellationToken ct);
}
