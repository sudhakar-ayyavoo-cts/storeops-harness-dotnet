using StoreOps.Domain.Alerts;

namespace StoreOps.Api.Contracts.Alerts;

public sealed class NotificationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public AlertType AlertType { get; init; }
    public NotificationChannel Channel { get; init; }
    public NotificationStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid? RelatedEntityId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? AcknowledgedAt { get; init; }

    public static NotificationDto FromDomain(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        AlertType = n.AlertType,
        Channel = n.Channel,
        Status = n.Status,
        Message = n.Message,
        RelatedEntityId = n.RelatedEntityId,
        CreatedAt = n.CreatedAt,
        AcknowledgedAt = n.AcknowledgedAt,
    };
}
