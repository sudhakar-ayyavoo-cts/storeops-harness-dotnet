namespace StoreOps.Application.Alerts;

public interface IAlertsEscalationSweepService
{
    Task<int> SweepAsync(CancellationToken ct);
}
