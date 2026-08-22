namespace StoreOps.Domain.Alerts;

public enum AlertType
{
    Inventory,
    SlaBreach,
    ShiftHandover,
    Escalation,
}

public enum NotificationChannel
{
    InApp,
    Email,
}

public enum NotificationStatus
{
    Unread,
    Read,
    Acknowledged,
}
