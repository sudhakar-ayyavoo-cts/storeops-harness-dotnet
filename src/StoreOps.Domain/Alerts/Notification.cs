namespace StoreOps.Domain.Alerts;

public sealed class Notification
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public AlertType AlertType { get; init; }
    public NotificationChannel Channel { get; init; }
    public NotificationStatus Status { get; set; }
    public string Message { get; init; } = string.Empty;
    public Guid? RelatedEntityId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
}
