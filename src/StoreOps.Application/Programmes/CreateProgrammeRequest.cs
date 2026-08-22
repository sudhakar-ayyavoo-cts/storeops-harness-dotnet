namespace StoreOps.Application.Programmes;

public sealed class CreateProgrammeRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid StoreId { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
}
