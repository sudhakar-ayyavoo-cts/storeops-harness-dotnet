using StoreOps.Domain.Alerts;

namespace StoreOps.Application.Alerts;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> ListAsync(Guid? userId, Guid? storeId, CancellationToken ct);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Notification> AddAsync(Notification notification, CancellationToken ct);
    Task<Notification> UpdateAsync(Notification notification, CancellationToken ct);
}
