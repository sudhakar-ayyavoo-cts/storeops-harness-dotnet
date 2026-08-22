using StoreOps.Domain.Programmes;

namespace StoreOps.Api.Contracts.Programmes;

public sealed class ProjectMemberDto
{
    public Guid UserId { get; init; }
    public ProjectRole Role { get; init; }

    public static ProjectMemberDto FromDomain(ProjectMember member) => new()
    {
        UserId = member.UserId,
        Role = member.Role,
    };
}
