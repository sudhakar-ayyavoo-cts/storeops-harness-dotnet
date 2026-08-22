using StoreOps.Domain.Reports;

namespace StoreOps.Api.Contracts.Reports;

public sealed class ReportDataDto
{
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int OverdueTasks { get; init; }
    public int ActiveProgrammes { get; init; }
    public int TotalStaff { get; init; }

    public static ReportDataDto FromDomain(ReportData data) => new()
    {
        TotalTasks = data.TotalTasks,
        CompletedTasks = data.CompletedTasks,
        OverdueTasks = data.OverdueTasks,
        ActiveProgrammes = data.ActiveProgrammes,
        TotalStaff = data.TotalStaff,
    };
}
