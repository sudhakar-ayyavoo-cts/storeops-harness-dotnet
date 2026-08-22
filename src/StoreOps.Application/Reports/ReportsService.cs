using StoreOps.Application.Activities;
using StoreOps.Application.Programmes;
using StoreOps.Application.Reports.Errors;
using StoreOps.Application.Staff;
using StoreOps.Domain.Reports;
using DomainTaskStatus = StoreOps.Domain.Activities.TaskStatus;

namespace StoreOps.Application.Reports;

public sealed class ReportsService : IReportsService
{
    private readonly IActivitiesService _activitiesService;
    private readonly IProgrammesService _programmesService;
    private readonly IStaffService _staffService;
    private readonly IReportRepository _reportRepository;

    public ReportsService(
        IActivitiesService activitiesService,
        IProgrammesService programmesService,
        IStaffService staffService,
        IReportRepository reportRepository)
    {
        _activitiesService = activitiesService;
        _programmesService = programmesService;
        _staffService = staffService;
        _reportRepository = reportRepository;
    }

    public async Task<Report> GetStoreSummaryAsync(Guid storeId, CancellationToken ct)
    {
        var tasks = await _activitiesService.ListAsync(null, storeId, ct);
        var programmes = await _programmesService.ListAsync(storeId, ct);
        var staff = await _staffService.ListAsync(storeId, ct);

        var now = DateTimeOffset.UtcNow;
        var overdueTasks = tasks.Count(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value < now &&
            t.Status != DomainTaskStatus.Done);

        var report = new Report
        {
            Id = Guid.NewGuid(),
            Type = ReportType.StoreSummary,
            Status = ReportStatus.Ready,
            StoreId = storeId,
            GeneratedAt = now,
            Data = new ReportData
            {
                TotalTasks = tasks.Count,
                CompletedTasks = tasks.Count(t => t.Status == DomainTaskStatus.Done),
                OverdueTasks = overdueTasks,
                ActiveProgrammes = programmes.Count,
                TotalStaff = staff.Count,
            },
        };

        return await _reportRepository.AddAsync(report, ct);
    }
}
