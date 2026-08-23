namespace StoreOps.Domain.Activities;

public sealed class StoreTask
{
    public Guid Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskCategory Category { get; set; }
    public Guid StoreId { get; init; }
    public Guid? AssignedToUserId { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SlaBreachedAt { get; set; }
}
