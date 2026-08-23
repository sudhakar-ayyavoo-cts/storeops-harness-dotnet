namespace StoreOps.Application.Activities;

public interface ISlaSweepService
{
    Task<int> SweepAsync(CancellationToken ct);
}
