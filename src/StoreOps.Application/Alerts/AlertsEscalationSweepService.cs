using Microsoft.Extensions.Options;
using StoreOps.Application.Activities;
using StoreOps.Application.Activities.Errors;
using StoreOps.Application.Common;
using StoreOps.Application.Staff;
using StoreOps.Domain.Alerts;
using StoreOps.Domain.Staff;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Application.Alerts;

public sealed class AlertsEscalationSweepService : IAlertsEscalationSweepService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IActivitiesService _activitiesService;
    private readonly IStaffService _staffService;
    private readonly IClock _clock;
    private readonly AlertsOptions _options;

    public AlertsEscalationSweepService(
        INotificationRepository notificationRepository,
        IActivitiesService activitiesService,
        IStaffService staffService,
        IClock clock,
        IOptions<AlertsOptions> options)
    {
        _notificationRepository = notificationRepository;
        _activitiesService = activitiesService;
        _staffService = staffService;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<int> SweepAsync(CancellationToken ct)
    {
        var all = await _notificationRepository.ListAsync(userId: null, storeId: null, ct);
        var now = _clock.UtcNow;
        var graceCutoff = now.AddHours(-_options.SlaEscalationGraceHours);

        var alreadyEscalatedTaskIds = all
            .Where(n => n.AlertType == AlertType.Escalation && n.RelatedEntityId.HasValue)
            .Select(n => n.RelatedEntityId!.Value)
            .ToHashSet();

        var candidates = all.Where(n =>
            n.AlertType == AlertType.SlaBreach &&
            n.Status != NotificationStatus.Acknowledged &&
            n.RelatedEntityId.HasValue &&
            n.CreatedAt <= graceCutoff &&
            !alreadyEscalatedTaskIds.Contains(n.RelatedEntityId!.Value));

        var escalatedCount = 0;

        foreach (var candidate in candidates)
        {
            var taskId = candidate.RelatedEntityId!.Value;

            StoreOps.Domain.Activities.StoreTask task;
            try
            {
                task = await _activitiesService.GetByIdAsync(taskId, ct);
            }
            catch (TaskNotFoundError)
            {
                continue;
            }

            if (task.Status == DomainTaskStatus.Done)
            {
                continue;
            }

            var staffInStore = await _staffService.ListAsync(task.StoreId, ct);
            var storeManager = staffInStore
                .Where(u => u.Role == StaffRole.StoreManager)
                .OrderBy(u => u.CreatedAt)
                .FirstOrDefault();

            if (storeManager is null)
            {
                continue;
            }

            await _notificationRepository.AddAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = storeManager.Id,
                AlertType = AlertType.Escalation,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Unread,
                Message = $"Task {taskId} SLA breach is unresolved beyond the grace period and has been escalated.",
                RelatedEntityId = taskId,
                CreatedAt = now,
            }, ct);

            escalatedCount++;
        }

        return escalatedCount;
    }
}
