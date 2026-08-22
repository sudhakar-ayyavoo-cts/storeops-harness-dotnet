using System.Collections.Concurrent;
using StoreOps.Application.Alerts;
using StoreOps.Domain.Alerts;

namespace StoreOps.Infrastructure.Alerts;

internal sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<Guid, Notification> _store = new();

    public Task<IReadOnlyList<Notification>> ListAsync(
        Guid? userId,
        Guid? storeId,
        CancellationToken ct)
    {
        var notifications = _store.Values.AsEnumerable();
        if (userId.HasValue)
        {
            notifications = notifications.Where(n => n.UserId == userId.Value);
        }

        return Task.FromResult<IReadOnlyList<Notification>>(notifications.ToList());
    }

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var notification);
        return Task.FromResult(notification);
    }

    public Task<Notification> AddAsync(Notification notification, CancellationToken ct)
    {
        _store[notification.Id] = notification;
        return Task.FromResult(notification);
    }

    public Task<Notification> UpdateAsync(Notification notification, CancellationToken ct)
    {
        _store[notification.Id] = notification;
        return Task.FromResult(notification);
    }
}
