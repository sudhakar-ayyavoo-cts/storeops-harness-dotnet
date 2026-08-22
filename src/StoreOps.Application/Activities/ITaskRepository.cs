using StoreOps.Domain.Activities;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Application.Activities;

public interface ITaskRepository
{
    Task<IReadOnlyList<StoreTask>> ListAsync(
        DomainTaskStatus? status,
        Guid? storeId,
        CancellationToken ct);

    Task<StoreTask?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<StoreTask> AddAsync(StoreTask task, CancellationToken ct);
}
