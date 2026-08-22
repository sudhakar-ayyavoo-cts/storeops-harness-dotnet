namespace StoreOps.Domain.Reports;

public sealed class ReportData
{
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int OverdueTasks { get; init; }
    public int ActiveProgrammes { get; init; }
    public int TotalStaff { get; init; }
}
