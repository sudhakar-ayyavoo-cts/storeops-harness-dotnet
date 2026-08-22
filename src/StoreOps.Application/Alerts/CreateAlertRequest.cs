using StoreOps.Domain.Alerts;

namespace StoreOps.Application.Alerts;

public sealed class CreateAlertRequest
{
    public Guid UserId { get; init; }
    public AlertType AlertType { get; init; }
    public NotificationChannel Channel { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid? RelatedEntityId { get; init; }
}
