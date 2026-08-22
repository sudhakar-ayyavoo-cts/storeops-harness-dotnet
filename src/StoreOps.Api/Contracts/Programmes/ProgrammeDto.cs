using StoreOps.Domain.Programmes;

namespace StoreOps.Api.Contracts.Programmes;

public sealed class ProgrammeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid StoreId { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<ProjectMemberDto> Members { get; init; } = Array.Empty<ProjectMemberDto>();

    public static ProgrammeDto FromDomain(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        StoreId = project.StoreId,
        StartDate = project.StartDate,
        EndDate = project.EndDate,
        CreatedAt = project.CreatedAt,
        Members = project.Members.Select(ProjectMemberDto.FromDomain).ToList(),
    };
}
