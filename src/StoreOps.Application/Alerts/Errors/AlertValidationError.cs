using StoreOps.Application.Common;

namespace StoreOps.Application.Alerts.Errors;

public sealed class AlertValidationError : AppError
{
    public override string Code => "ALERT_VALIDATION_ERROR";
    public override int StatusCode => 422;

    public AlertValidationError(string message) : base(message)
    {
    }
}
