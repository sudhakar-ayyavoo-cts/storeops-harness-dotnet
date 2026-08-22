namespace StoreOps.Domain.Programmes;

public sealed class Project
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid StoreId { get; init; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public List<ProjectMember> Members { get; init; } = new();
}
