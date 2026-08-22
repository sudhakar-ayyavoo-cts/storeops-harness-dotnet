namespace StoreOps.Domain.Staff;

public sealed class AuthToken
{
    public string Token { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}
