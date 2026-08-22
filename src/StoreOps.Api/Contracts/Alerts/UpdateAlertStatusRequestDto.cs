using StoreOps.Domain.Alerts;

namespace StoreOps.Api.Contracts.Alerts;

public sealed class UpdateAlertStatusRequestDto
{
    public NotificationStatus Status { get; init; }
}
