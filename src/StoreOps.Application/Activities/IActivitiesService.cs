using StoreOps.Domain.Activities;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Application.Activities;

public interface IActivitiesService
{
    Task<IReadOnlyList<StoreTask>> ListAsync(
        DomainTaskStatus? status,
        Guid? storeId,
        CancellationToken ct);

    Task<StoreTask> CreateAsync(CreateTaskRequest request, CancellationToken ct);

    Task<StoreTask> GetByIdAsync(Guid id, CancellationToken ct);
}
