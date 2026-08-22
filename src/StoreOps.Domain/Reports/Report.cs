namespace StoreOps.Domain.Reports;

public sealed class Report
{
    public Guid Id { get; init; }
    public ReportType Type { get; init; }
    public ReportStatus Status { get; set; }
    public Guid StoreId { get; init; }
    public string? RegionId { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public ReportData? Data { get; set; }
}
