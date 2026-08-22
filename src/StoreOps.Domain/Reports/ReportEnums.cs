namespace StoreOps.Domain.Reports;

public enum ReportType
{
    StoreSummary,
    RegionalRollup,
    DepartmentPerformance,
}

public enum ReportStatus
{
    Pending,
    Ready,
    Failed,
}
