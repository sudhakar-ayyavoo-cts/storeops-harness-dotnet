namespace StoreOps.Domain.Events;

public sealed record SlaBreachEvent(
    Guid TaskId,
    Guid AssignedToUserId,
    Guid DepartmentLeadId,
    DateTimeOffset BreachedAt) : IDomainEvent;
