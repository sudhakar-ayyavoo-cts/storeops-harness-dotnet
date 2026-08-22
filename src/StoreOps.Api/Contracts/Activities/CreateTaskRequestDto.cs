using StoreOps.Domain.Activities;

namespace StoreOps.Api.Contracts.Activities;

public sealed class CreateTaskRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskCategory Category { get; init; }
    public Guid StoreId { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public DateTimeOffset? DueDate { get; init; }
}
