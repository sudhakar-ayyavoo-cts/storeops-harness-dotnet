namespace StoreOps.Domain.Programmes;

public sealed class ProjectMember
{
    public Guid UserId { get; init; }
    public ProjectRole Role { get; set; }
}
