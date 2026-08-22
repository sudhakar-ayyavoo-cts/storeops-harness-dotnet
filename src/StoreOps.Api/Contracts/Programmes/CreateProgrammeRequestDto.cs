namespace StoreOps.Api.Contracts.Programmes;

public sealed class CreateProgrammeRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid StoreId { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
}
