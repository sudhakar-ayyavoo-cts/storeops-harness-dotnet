using StoreOps.Domain.Activities;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Api.Contracts.Activities;

public sealed class TaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DomainTaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskCategory Category { get; init; }
    public Guid StoreId { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public static TaskDto FromDomain(StoreTask task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        Priority = task.Priority,
        Category = task.Category,
        StoreId = task.StoreId,
        AssignedToUserId = task.AssignedToUserId,
        DueDate = task.DueDate,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
    };
}
