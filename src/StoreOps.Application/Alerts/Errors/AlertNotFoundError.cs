using StoreOps.Application.Common;

namespace StoreOps.Application.Alerts.Errors;

public sealed class AlertNotFoundError : AppError
{
    public override string Code => "ALERT_NOT_FOUND";
    public override int StatusCode => 404;

    public AlertNotFoundError(Guid id) : base($"Alert {id} was not found.")
    {
    }
}
