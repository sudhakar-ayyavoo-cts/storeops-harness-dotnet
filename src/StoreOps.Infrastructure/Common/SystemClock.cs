using StoreOps.Application.Common;

namespace StoreOps.Infrastructure.Common;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
