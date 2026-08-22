using StoreOps.Application.Common;

namespace StoreOps.Application.Reports.Errors;

public sealed class ReportNotFoundError : AppError
{
    public override string Code => "REPORT_NOT_FOUND";
    public override int StatusCode => 404;

    public ReportNotFoundError(Guid storeId) : base($"Report for store {storeId} was not found.")
    {
    }
}
