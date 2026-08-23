using StoreOps.Application.Common;
using StoreOps.Application.Staff;
using StoreOps.Domain.Activities;
using StoreOps.Domain.Events;
using StoreOps.Domain.Staff;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Application.Activities;

public sealed class SlaSweepService : ISlaSweepService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IStaffService _staffService;
    private readonly IEventBus _eventBus;
    private readonly IClock _clock;

    public SlaSweepService(
        ITaskRepository taskRepository,
        IStaffService staffService,
        IEventBus eventBus,
        IClock clock)
    {
        _taskRepository = taskRepository;
        _staffService = staffService;
        _eventBus = eventBus;
        _clock = clock;
    }

    public async Task<int> SweepAsync(CancellationToken ct)
    {
        var tasks = await _taskRepository.ListAsync(status: null, storeId: null, ct);
        var now = _clock.UtcNow;

        var breachingTasks = tasks.Where(t =>
            (t.Priority == TaskPriority.High || t.Priority == TaskPriority.Critical) &&
            t.Status != DomainTaskStatus.Done &&
            t.DueDate.HasValue &&
            t.DueDate.Value < now &&
            t.SlaBreachedAt is null);

        var breachCount = 0;

        foreach (var task in breachingTasks)
        {
            var staffInStore = await _staffService.ListAsync(task.StoreId, ct);
            var departmentLead = staffInStore
                .Where(u => u.Role == StaffRole.DepartmentLead)
                .OrderBy(u => u.CreatedAt)
                .FirstOrDefault();

            if (departmentLead is null)
            {
                continue;
            }

            _eventBus.Publish(new SlaBreachEvent(
                task.Id,
                task.AssignedToUserId ?? Guid.Empty,
                departmentLead.Id,
                now));

            task.SlaBreachedAt = now;
            await _taskRepository.UpdateAsync(task, ct);

            breachCount++;
        }

        return breachCount;
    }
}
