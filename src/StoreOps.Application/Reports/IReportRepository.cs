using StoreOps.Domain.Reports;

namespace StoreOps.Application.Reports;

public interface IReportRepository
{
    Task<Report?> GetByStoreIdAsync(Guid storeId, CancellationToken ct);
    Task<Report> AddAsync(Report report, CancellationToken ct);
}
