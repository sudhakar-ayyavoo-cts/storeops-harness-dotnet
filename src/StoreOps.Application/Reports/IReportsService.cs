using StoreOps.Domain.Reports;

namespace StoreOps.Application.Reports;

public interface IReportsService
{
    Task<Report> GetStoreSummaryAsync(Guid storeId, CancellationToken ct);
}
