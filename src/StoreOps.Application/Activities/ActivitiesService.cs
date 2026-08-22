using StoreOps.Application.Activities.Errors;
using StoreOps.Application.Staff;
using StoreOps.Domain.Activities;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Application.Activities;

public sealed class ActivitiesService : IActivitiesService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IStaffService _staffService;

    public ActivitiesService(ITaskRepository taskRepository, IStaffService staffService)
    {
        _taskRepository = taskRepository;
        _staffService = staffService;
    }

    public async Task<IReadOnlyList<StoreTask>> ListAsync(
        DomainTaskStatus? status,
        Guid? storeId,
        CancellationToken ct)
        => await _taskRepository.ListAsync(status, storeId, ct);

    public async Task<StoreTask> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var task = await _taskRepository.GetByIdAsync(id, ct);
        if (task is null)
        {
            throw new TaskNotFoundError(id);
        }

        return task;
    }

    public async Task<StoreTask> CreateAsync(CreateTaskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new TaskValidationError("Title is required.");
        }

        if (request.StoreId == Guid.Empty)
        {
            throw new TaskValidationError("StoreId is required.");
        }

        if (request.AssignedToUserId.HasValue)
        {
            var staff = await _staffService.GetByIdAsync(request.AssignedToUserId.Value, ct);
            if (staff is null)
            {
                throw new TaskValidationError($"Assigned user {request.AssignedToUserId} does not exist.");
            }
        }

        var task = new StoreTask
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Status = DomainTaskStatus.Todo,
            Priority = request.Priority,
            Category = request.Category,
            StoreId = request.StoreId,
            AssignedToUserId = request.AssignedToUserId,
            DueDate = request.DueDate,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        return await _taskRepository.AddAsync(task, ct);
    }
}
