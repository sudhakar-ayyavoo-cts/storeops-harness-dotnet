using System.Collections.Concurrent;
using StoreOps.Application.Reports;
using StoreOps.Domain.Reports;

namespace StoreOps.Infrastructure.Reports;

internal sealed class InMemoryReportRepository : IReportRepository
{
    private readonly ConcurrentDictionary<Guid, Report> _store = new();

    public Task<Report?> GetByStoreIdAsync(Guid storeId, CancellationToken ct)
    {
        var report = _store.Values
            .Where(r => r.StoreId == storeId)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefault();
        return Task.FromResult(report);
    }

    public Task<Report> AddAsync(Report report, CancellationToken ct)
    {
        _store[report.Id] = report;
        return Task.FromResult(report);
    }
}
