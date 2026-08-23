namespace StoreOps.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
