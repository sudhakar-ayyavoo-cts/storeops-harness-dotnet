namespace StoreOps.Domain.Activities;

public enum TaskStatus
{
    Todo,
    InProgress,
    Done,
    Blocked,
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical,
}

public enum TaskCategory
{
    Restocking,
    Planogram,
    Audit,
    Compliance,
    General,
}
