using StoreOps.Domain.Reports;

namespace StoreOps.Api.Contracts.Reports;

public sealed class ReportDto
{
    public Guid Id { get; init; }
    public ReportType Type { get; init; }
    public ReportStatus Status { get; init; }
    public Guid StoreId { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public ReportDataDto? Data { get; init; }

    public static ReportDto FromDomain(Report report) => new()
    {
        Id = report.Id,
        Type = report.Type,
        Status = report.Status,
        StoreId = report.StoreId,
        GeneratedAt = report.GeneratedAt,
        Data = report.Data is not null ? ReportDataDto.FromDomain(report.Data) : null,
    };
}
