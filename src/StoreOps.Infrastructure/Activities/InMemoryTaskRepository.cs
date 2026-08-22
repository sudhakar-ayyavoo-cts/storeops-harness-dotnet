using System.Collections.Concurrent;
using StoreOps.Application.Activities;
using StoreOps.Domain.Activities;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Infrastructure.Activities;

internal sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<Guid, StoreTask> _store = new();

    public Task<IReadOnlyList<StoreTask>> ListAsync(
        DomainTaskStatus? status,
        Guid? storeId,
        CancellationToken ct)
    {
        var tasks = _store.Values.AsEnumerable();

        if (status.HasValue)
        {
            tasks = tasks.Where(t => t.Status == status.Value);
        }

        if (storeId.HasValue)
        {
            tasks = tasks.Where(t => t.StoreId == storeId.Value);
        }

        return Task.FromResult<IReadOnlyList<StoreTask>>(tasks.ToList());
    }

    public Task<StoreTask?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task<StoreTask> AddAsync(StoreTask task, CancellationToken ct)
    {
        _store[task.Id] = task;
        return Task.FromResult(task);
    }
}
